-- RLS: неавторизованные (anon) — только чтение, авторизованные (Supabase Auth) — полный доступ

-- employee
ALTER TABLE employee ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_employee" ON employee FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_employee" ON employee FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- faculty
ALTER TABLE faculty ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_faculty" ON faculty FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_faculty" ON faculty FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- department
ALTER TABLE department ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_department" ON department FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_department" ON department FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- section
ALTER TABLE section ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_section" ON section FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_section" ON section FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- specialty
ALTER TABLE specialty ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_specialty" ON specialty FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_specialty" ON specialty FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- discipline
ALTER TABLE discipline ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_discipline" ON discipline FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_discipline" ON discipline FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- curriculum_item
ALTER TABLE curriculum_item ENABLE ROW LEVEL SECURITY;

CREATE POLICY "anon_select_curriculum_item" ON curriculum_item FOR SELECT TO anon USING (true);
CREATE POLICY "authenticated_all_curriculum_item" ON curriculum_item FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- app_readonly: login for app/tests (DefaultConnection). NOINHERIT + GRANT anon/authenticated so
-- SupabaseRlsInterceptor can SET LOCAL role; direct session is read-only (SELECT policies only).
CREATE ROLE app_readonly WITH LOGIN PASSWORD 'app_readonly_local' NOINHERIT;

GRANT anon TO app_readonly;
GRANT authenticated TO app_readonly;

GRANT CONNECT ON DATABASE postgres TO app_readonly;
GRANT USAGE ON SCHEMA public TO app_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT SELECT ON TABLES TO app_readonly;

CREATE POLICY "app_readonly_select_employee" ON employee FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_faculty" ON faculty FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_department" ON department FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_section" ON section FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_specialty" ON specialty FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_discipline" ON discipline FOR SELECT TO app_readonly USING (true);
CREATE POLICY "app_readonly_select_curriculum_item" ON curriculum_item FOR SELECT TO app_readonly USING (true);
