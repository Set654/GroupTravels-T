import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, Tabs, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
import { isValidEmail, isValidPhone, normalizePhone, validatePassword } from '../lib/validators.js';

export default function RegisterPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  function submit(event) {
    event.preventDefault();
    if (!isValidEmail(email)) return window.alert('Введите корректный Email.');
    if (phone.trim() && !isValidPhone(phone)) return window.alert('Введите корректный номер телефона.');
    const error = validatePassword(password);
    if (error) return window.alert(error);
    if (password !== confirmPassword) return window.alert('Пароли не совпадают.');
    navigate('/acquaintance', {
      state: { registration: { email: email.trim(), phone: phone.trim() ? normalizePhone(phone) : '', password } },
    });
  }

  return <AuthCard variant="register">
    <PageHeading title="Создайте аккаунт">Присоединяйтесь и планируйте поездки вместе с друзьями.</PageHeading>
    <Tabs className="auth-main-tabs" value="register" onChange={v => v === 'login' && navigate('/login')}
      options={[{ value: 'login', label: 'Вход' }, { value: 'register', label: 'Регистрация' }]} />
    <form className="register-form" onSubmit={submit} noValidate>
      <FormField label="Email" type="email" autoComplete="email" value={email}
        placeholder="Например, makarena@mail.ru" onChange={e => setEmail(e.target.value)} />
      <FormField className="register-phone" label="Номер телефона" type="tel" autoComplete="tel" value={phone}
        placeholder="Например, +7 800 555-35-35" onChange={e => setPhone(e.target.value)} />
      <FormField className="register-password" label="Пароль" autoComplete="new-password"
        type={showPassword ? 'text' : 'password'} value={password} placeholder="Не менее 8 символов"
        onChange={e => setPassword(e.target.value)} toggleVisible={showPassword}
        onToggleVisible={() => setShowPassword(v => !v)} />
      <FormField className="register-confirm" label="Повторите пароль" autoComplete="new-password"
        type={showConfirm ? 'text' : 'password'} value={confirmPassword} placeholder="Повторите пароль"
        onChange={e => setConfirmPassword(e.target.value)} toggleVisible={showConfirm}
        onToggleVisible={() => setShowConfirm(v => !v)} />
      <CenterActions className="register-submit">
        <ActionButton type="submit">Зарегистрироваться</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
