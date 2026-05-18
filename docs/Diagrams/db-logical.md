# Логическая диаграмма БД

Концептуальный уровень (сущности, ромбы типов связей, однонаправленные стрелки — см. [db-conceptual.md](db-conceptual.md), [db-conceptual.puml](db-conceptual.puml)).

Логическая модель данных предметной области методического отдела: сущности, их ключевые атрибуты и связи без технических деталей хранения.

Тот же состав сущностей и связей в PlantUML (IE-диаграмма, атрибуты без типов данных): [db-logical.puml](db-logical.puml).

```mermaid
erDiagram
    Faculty ||--|| Employee : "декан"
    Faculty ||--o{ Department : "входит"
    Department ||--|| Employee : "заведующий"
    Department ||--o{ Employee : "сотрудники"
    Department ||--o{ Section : "секции"
    Section }o--|| Employee : "руководитель"
    Section ||--o{ Employee : "сотрудники секции"
    Department ||--o{ Discipline : "преподает"
    Specialty ||--o{ CurriculumItem : "включена"
    Discipline ||--o{ CurriculumItem : "входит"

    Faculty {
        x id PK
        x name "название факультета"
        x dean_id FK "декан"
    }

    Employee {
        x id PK
        x surname "фамилия"
        x name "имя"
        x patronymic "отчество"
        x degree "учёная степень"
        x title "учёное звание"
        x phone "телефон"
        x email "почта"
        x department_id FK "кафедра (для сотрудников кафедры)"
        x section_id FK "секция (опционально)"
    }

    Section {
        x id PK
        x department_id FK
        x name "название секции"
        x head_id FK "руководитель (опционально)"
        x phones "телефон(ы)"
    }

    Specialty {
        x id PK
        x code "код специальности"
        x name "название"
        x qualification "присваиваемая квалификация"
        x duration "продолжительность (лет)"
        x form_of_study "дневная, вечерняя, заочная"
    }

    Department {
        x id PK
        x faculty_id FK
        x head_id FK "заведующий кафедрой"
        x name "название кафедры"
        x phones "телефон(ы)"
    }

    Discipline {
        x id PK
        x department_id FK
        x name "название дисциплины"
    }

    CurriculumItem {
        x id PK
        x discipline_id FK
        x specialty_id FK
        x semester "семестр"
        x lecture_hours "часы лекций"
        x lab_hours "часы лабораторных"
        x usr_hours "часы УСР"
        x practical_hours "часы практических"
        x has_course_project "курсовой проект"
        x is_credit "зачёт с оценкой"
        x is_exam "экзамен"
    }

    AuditLog {
        x id PK
        x occurred_at "время события"
        x actor_user_id "идентификатор пользователя"
        x actor_display_name "имя пользователя"
        x action "тип действия"
        x table_name "таблица сущности"
        x entity_id "id измененной строки"
        x old_row "состояние до изменения"
        x new_row "состояние после изменения"
    }
```



