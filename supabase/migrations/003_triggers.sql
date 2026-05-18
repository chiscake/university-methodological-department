-- Триггеры целостности (бизнес-логика по структуре вуза РБ)

-- 1. Сотрудник–секция: секция должна относиться к кафедре сотрудника
CREATE OR REPLACE FUNCTION check_employee_section_department()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.section_id IS NOT NULL AND NEW.department_id IS NOT NULL THEN
        IF (SELECT department_id FROM section WHERE id = NEW.section_id) IS DISTINCT FROM NEW.department_id THEN
            RAISE EXCEPTION 'Секция должна относиться к кафедре сотрудника';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_employee_section_department
    BEFORE INSERT OR UPDATE OF section_id, department_id ON employee
    FOR EACH ROW EXECUTE PROCEDURE check_employee_section_department();

-- 2. Заведующий кафедрой должен быть сотрудником этой кафедры
--    (при назначении допускается department_id = NULL, затем привязка через UPDATE employee)
CREATE OR REPLACE FUNCTION check_department_head_belongs()
RETURNS TRIGGER AS $$
DECLARE
    emp_dept_id INTEGER;
BEGIN
    IF NEW.head_id IS NOT NULL THEN
        SELECT department_id INTO emp_dept_id FROM employee WHERE id = NEW.head_id;
        IF emp_dept_id IS NOT NULL AND emp_dept_id IS DISTINCT FROM NEW.id THEN
            RAISE EXCEPTION 'Заведующий кафедрой должен быть сотрудником этой кафедры (employee.department_id = department.id)';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_department_head_belongs
    BEFORE INSERT OR UPDATE OF head_id ON department
    FOR EACH ROW EXECUTE PROCEDURE check_department_head_belongs();

-- 2b. Смена кафедры у сотрудника: если он заведующий, новая кафедра должна совпадать с его кафедрой
CREATE OR REPLACE FUNCTION check_employee_department_if_head()
RETURNS TRIGGER AS $$
DECLARE
    dept_id INTEGER;
BEGIN
    IF NEW.department_id IS DISTINCT FROM OLD.department_id THEN
        SELECT id INTO dept_id FROM department WHERE head_id = NEW.id;
        IF dept_id IS NOT NULL AND NEW.department_id IS DISTINCT FROM dept_id THEN
            RAISE EXCEPTION 'Нельзя перевести заведующего кафедрой в другую кафедру; сначала назначьте другого заведующего';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_employee_department_if_head
    BEFORE UPDATE OF department_id ON employee
    FOR EACH ROW EXECUTE PROCEDURE check_employee_department_if_head();

-- 3. Руководитель секции: если у сотрудника ещё не указаны кафедра/секция — назначаем автоматически, иначе ошибка
CREATE OR REPLACE FUNCTION check_section_head_belongs_or_unassigned()
RETURNS TRIGGER AS $$
DECLARE
    emp_section_id INTEGER;
    emp_dept_id INTEGER;
BEGIN
    IF NEW.head_id IS NOT NULL THEN
        SELECT section_id, department_id INTO emp_section_id, emp_dept_id
        FROM employee WHERE id = NEW.head_id;
        -- Уже привязан к другой секции/кафедре — ошибка
        IF (emp_section_id IS NOT NULL OR emp_dept_id IS NOT NULL)
           AND (emp_section_id IS DISTINCT FROM NEW.id OR emp_dept_id IS DISTINCT FROM NEW.department_id) THEN
            RAISE EXCEPTION 'Руководитель секции уже привязан к другой кафедре/секции; назначьте сотрудника без кафедры/секции или смените привязку вручную';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_section_head_belongs_or_unassigned
    BEFORE INSERT OR UPDATE OF head_id ON section
    FOR EACH ROW EXECUTE PROCEDURE check_section_head_belongs_or_unassigned();

-- Автоназначение: если кафедра/секция у сотрудника не указаны — привязываем к этой секции (AFTER — секция уже существует)
CREATE OR REPLACE FUNCTION assign_section_head_to_section()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.head_id IS NOT NULL THEN
        UPDATE employee
        SET section_id = NEW.id,
            department_id = NEW.department_id
        WHERE id = NEW.head_id
          AND section_id IS NULL
          AND department_id IS NULL;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_section_assign_head
    AFTER INSERT OR UPDATE OF head_id ON section
    FOR EACH ROW EXECUTE PROCEDURE assign_section_head_to_section();

-- 4. Лимит предметов в семестре по специальности: не более 7 записей curriculum_item
--    в одной паре (specialty_id, semester). Маркер для распознавания на клиенте — HINT.
CREATE OR REPLACE FUNCTION check_curriculum_item_semester_limit()
RETURNS TRIGGER AS $$
DECLARE
    v_existing INTEGER;
    v_limit CONSTANT INTEGER := 7;
BEGIN
    IF TG_OP = 'INSERT' THEN
        SELECT COUNT(*) INTO v_existing
        FROM curriculum_item
        WHERE specialty_id = NEW.specialty_id
          AND semester = NEW.semester;

        IF v_existing >= v_limit THEN
            RAISE EXCEPTION
                'Нельзя добавить предмет: в выбранном семестре для данной специальности уже % предметов.', v_limit
                USING HINT = 'CURRICULUM_LIMIT_EXCEEDED';
        END IF;
    ELSIF TG_OP = 'UPDATE' THEN
        -- Проверяем только при переносе записи в другую (specialty, semester)-группу.
        IF NEW.specialty_id IS DISTINCT FROM OLD.specialty_id
           OR NEW.semester IS DISTINCT FROM OLD.semester THEN
            SELECT COUNT(*) INTO v_existing
            FROM curriculum_item
            WHERE specialty_id = NEW.specialty_id
              AND semester = NEW.semester
              AND id <> NEW.id;

            IF v_existing >= v_limit THEN
                RAISE EXCEPTION
                    'Нельзя перенести предмет: в выбранном семестре для данной специальности уже % предметов.', v_limit
                    USING HINT = 'CURRICULUM_LIMIT_EXCEEDED';
            END IF;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_curriculum_item_semester_limit
    BEFORE INSERT OR UPDATE OF specialty_id, semester ON curriculum_item
    FOR EACH ROW EXECUTE PROCEDURE check_curriculum_item_semester_limit();

-- 5. Форма контроля curriculum_item:
--    1) Запрещено одновременно хранить зачёт и экзамен.
--    2) При курсовом проекте автоматически нормализуем форму контроля к экзамену.
CREATE OR REPLACE FUNCTION normalize_and_validate_curriculum_item_control_form()
RETURNS TRIGGER AS $$
BEGIN
    -- Нормализация: при курсовом проекте допускается только экзамен.
    IF NEW.has_course_project THEN
        NEW.is_exam := TRUE;
        NEW.is_credit := FALSE;
    END IF;

    -- Валидация: конфликт форм контроля недопустим.
    IF NEW.is_credit AND NEW.is_exam THEN
        RAISE EXCEPTION
            'Некорректная форма контроля: нельзя одновременно указать зачёт и экзамен.';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_curriculum_item_control_form_model
    BEFORE INSERT OR UPDATE OF has_course_project, is_credit, is_exam ON curriculum_item
    FOR EACH ROW EXECUTE PROCEDURE normalize_and_validate_curriculum_item_control_form();
