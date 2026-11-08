const API_BASE = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '');
export const DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true';
console.log('DEMO_MODE:', DEMO_MODE);
console.log('ENV VALUE:', import.meta.env.VITE_DEMO_MODE);

const delay = (ms = 400) => new Promise((resolve) => setTimeout(resolve, ms));

async function post(path, body) {
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  } catch {
    throw new Error('Нет соединения с сервером. Проверьте, что .NET API запущен и адрес API настроен.');
  }

  const raw = await response.text();
  let data = {};
  try { data = raw ? JSON.parse(raw) : {}; } catch { data = {}; }

  if (!response.ok) {
    const detail = data?.message || data?.title || data?.error || data?.detail;
    const explanation = typeof detail === 'string' ? detail : null;
    throw new Error(explanation || ({ 400: 'Проверьте введённые данные.', 401: 'Неверные данные для входа.', 409: 'Такой пользователь уже зарегистрирован.', 429: 'Слишком много попыток. Попробуйте позже.' }[response.status] || `Ошибка сервера (${response.status}).`));
  }
  return data;
}

export const authApi = {
  async login(email, password) {
    if (DEMO_MODE) { await delay(); return { accessToken: 'demo-token', user: { email } }; }
    return post('/auth/login', { email, password });
  },
  async register({ name, email, phone, password }) {
    if (DEMO_MODE) { await delay(); return { message: 'Аккаунт создан' }; }
    return post('/auth/register', { name, email, ...(phone ? { phone } : {}), password });
  },
  async sendPhoneCode(phone) {
    if (DEMO_MODE) { await delay(); return { message: 'Демо-код: 1234' }; }
    return post('/auth/phone/send-code', { phone });
  },
  async verifyPhoneCode(phone, code) {
    if (DEMO_MODE) {
      await delay();
      if (code !== '1234') throw new Error('В демо-режиме используйте код 1234.');
      return { accessToken: 'demo-token', user: { phone } };
    }
    return post('/auth/phone/verify-code', { phone, code });
  },
  async requestPasswordReset(contact, type) {
    if (DEMO_MODE) { await delay(); return { message: 'Инструкция отправлена' }; }
    return post('/auth/reset-password', { [type]: contact });
  },
  async verifyResetCode(contact, type, code) {
    if (DEMO_MODE) {
      await delay();
      if (code !== '123456') throw new Error('В демо-режиме используйте код 123456.');
      return { resetToken: 'demo-reset-token' };
    }
    const path = import.meta.env.VITE_RESET_VERIFY_PATH;
    if (!path) throw new Error('В SRS не указан API проверки кода сброса пароля. Согласуйте маршрут с бэкендом и задайте VITE_RESET_VERIFY_PATH.');
    return post(path, { [type]: contact, code });
  },
  async updatePassword(resetToken, password) {
    if (DEMO_MODE) { await delay(); return { message: 'Пароль изменён' }; }
    const path = import.meta.env.VITE_RESET_CONFIRM_PATH;
    if (!path) throw new Error('В SRS не указан API установки нового пароля. Согласуйте маршрут с бэкендом и задайте VITE_RESET_CONFIRM_PATH.');
    return post(path, { resetToken, newPassword: password });
  },
};
