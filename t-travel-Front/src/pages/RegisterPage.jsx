import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { SvgStage, SvgInput, Hotspot } from '../components/SvgStage.jsx';
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

  return <SvgStage name="Registration.svg" height={624}>
    <Hotspot x={32} y={112} w={233} h={40} label="Вход" onClick={() => navigate('/login')} />
    <Hotspot x={285} y={112} w={233} h={40} label="Регистрация" />
    <form className="svg-stage-form" noValidate onSubmit={submit}>
      <SvgInput x={32} y={206} w={486} label="Email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
      <SvgInput x={32} y={296} w={486} label="Номер телефона" type="tel" autoComplete="tel" value={phone} onChange={(e) => setPhone(e.target.value)} />
      <SvgInput x={32} y={398} w={446} label="Пароль" type={showPassword ? 'text' : 'password'} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
      <Hotspot x={474} y={399} w={36} h={46} label={showPassword ? 'Скрыть пароль' : 'Показать пароль'} onClick={() => setShowPassword(!showPassword)} />
      <SvgInput x={32} y={488} w={446} label="Повторите пароль" type={showConfirm ? 'text' : 'password'} autoComplete="new-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
      <Hotspot x={474} y={489} w={36} h={46} label={showConfirm ? 'Скрыть пароль' : 'Показать пароль'} onClick={() => setShowConfirm(!showConfirm)} />
      <Hotspot x={178} y={560} w={194} h={40} label="Зарегистрироваться" type="submit" />
    </form>
  </SvgStage>;
}
