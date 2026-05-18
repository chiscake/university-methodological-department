/**
 * Добавляет тестовых пользователей в Supabase Auth:
 * - test@example.com / 12345678 (обычный пользователь)
 * - admin@example.com / 12345678 (администратор: app_metadata.role=admin)
 * Использует SUPABASE_URL и SUPABASE_SERVICE_ROLE_KEY из env или из `supabase status -o env`.
 */

const { createClient } = require('@supabase/supabase-js');
const { execSync } = require('child_process');
const path = require('path');

const TEST_EMAIL = 'test@example.com';
const ADMIN_EMAIL = 'admin@example.com';
const DEFAULT_PASSWORD = '12345678';

function getEnvFromSupabaseStatus() {
  const cwd = path.join(__dirname, '..');
  const out = execSync('npx supabase status -o env', { encoding: 'utf8', cwd });
  const env = {};
  for (const line of out.split(/\r?\n/)) {
    const idx = line.indexOf('=');
    if (idx === -1) continue;
    const key = line.slice(0, idx).trim();
    let value = line.slice(idx + 1).trim();
    if (value.length >= 2 && value.startsWith('"') && value.endsWith('"')) {
      value = value.slice(1, -1);
    }
    if (key === 'API_URL' || key === 'SERVICE_ROLE_KEY') env[key] = value;
  }
  return env;
}

async function main() {
  let url = process.env.SUPABASE_URL;
  let serviceRoleKey = process.env.SUPABASE_SERVICE_ROLE_KEY;

  if (!url || !serviceRoleKey) {
    console.log('SUPABASE_URL или SUPABASE_SERVICE_ROLE_KEY не заданы, получаю из supabase status...');
    const statusEnv = getEnvFromSupabaseStatus();
    url = url || statusEnv.API_URL;
    serviceRoleKey = serviceRoleKey || statusEnv.SERVICE_ROLE_KEY;
  }

  if (!url || !serviceRoleKey) {
    console.error('Не удалось получить URL и/или service role key. Запустите supabase start и повторите.');
    process.exit(1);
  }

  const supabase = createClient(url, serviceRoleKey, { auth: { autoRefreshToken: false, persistSession: false } });
  const listOptions = { page: 1, perPage: 1000 };

  await ensureUser(supabase, listOptions, {
    email: TEST_EMAIL,
    password: DEFAULT_PASSWORD,
    appMetadata: { role: 'user' },
    userMetadata: { full_name: 'Тестовый Пользователь' },
    displayName: 'test'
  });

  await ensureUser(supabase, listOptions, {
    email: ADMIN_EMAIL,
    password: DEFAULT_PASSWORD,
    appMetadata: { role: 'admin' },
    userMetadata: { full_name: 'Тестовый Администратор' },
    displayName: 'admin'
  });
}

async function ensureUser(supabase, listOptions, user) {
  const { data, error } = await supabase.auth.admin.createUser({
    email: user.email,
    password: user.password,
    email_confirm: true,
    app_metadata: user.appMetadata,
    user_metadata: user.userMetadata
  });

  if (!error) {
    console.log('Пользователь создан:', data.user?.email || user.email);
    return;
  }

  if (!error.message || !error.message.includes('already been registered')) {
    console.error('Ошибка создания пользователя:', error.message);
    process.exit(1);
  }

  const { data: usersData, error: listError } = await supabase.auth.admin.listUsers(listOptions);
  if (listError) {
    console.error('Ошибка получения списка пользователей:', listError.message);
    process.exit(1);
  }

  const existing = usersData?.users?.find((u) => (u.email || '').toLowerCase() === user.email.toLowerCase());
  if (!existing) {
    console.error(`Пользователь ${user.email} существует, но не найден через listUsers.`);
    process.exit(1);
  }

  const mergedMetadata = { ...(existing.app_metadata || {}), ...(user.appMetadata || {}) };
  const mergedUserMetadata = { ...(existing.user_metadata || {}), ...(user.userMetadata || {}) };
  const { error: updateError } = await supabase.auth.admin.updateUserById(existing.id, {
    app_metadata: mergedMetadata,
    user_metadata: mergedUserMetadata
  });

  if (updateError) {
    console.error(`Ошибка обновления app_metadata для ${user.displayName}:`, updateError.message);
    process.exit(1);
  }

  console.log(`Пользователь ${user.email} уже существовал, app_metadata и user_metadata обновлены.`);
}

main();
