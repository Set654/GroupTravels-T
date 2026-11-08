export function isValidEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function normalizePhone(value) {
  let digits = value.replace(/\D/g, '');
  if (digits.length === 11 && digits[0] === '8') digits = `7${digits.slice(1)}`;
  if (digits.length === 10) digits = `7${digits}`;
  return `+${digits}`;
}

export function isValidPhone(value) {
  const phone = normalizePhone(value);
  return /^\+[1-9]\d{9,14}$/.test(phone);
}

export function validatePassword(password) {
  return password.length >= 8 ? '' : 'Пароль должен содержать не менее 8 символов.';
}
