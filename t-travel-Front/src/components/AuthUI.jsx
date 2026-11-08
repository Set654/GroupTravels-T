import { useId } from 'react';

// Все элементы настоящие HTML-элементы: никакого SVG-фона и прозрачных Hotspot.
export function AuthCard({ variant, children }) {
  return <main className={`auth-card auth-card--${variant}`}>{children}</main>;
}

export function PageHeading({ title, children }) {
  return <header className="page-heading">
    <h1>{title}</h1>
    {children && <p>{children}</p>}
  </header>;
}

export function Tabs({ options, value, onChange, className = '' }) {
  return <div className={`choice-tabs ${className}`} role="group" aria-label="Выбор варианта">
    {options.map(({ value: optionValue, label }) =>
      <button key={optionValue} type="button" className={`choice-tab ${value === optionValue ? 'is-active' : ''}`}
        aria-pressed={value === optionValue} onClick={() => onChange(optionValue)}>{label}</button>
    )}
  </div>;
}

export function FormField({ label, value, onChange, placeholder = '', type = 'text',
  autoComplete, inputMode, maxLength, className = '', rightLink, rightLinkClick,
  toggleVisible, onToggleVisible, disabled }) {
  const inputId = useId();
  return <div className={`form-field ${className}`}>
    <div className="field-heading">
      <label htmlFor={inputId}>{label}</label>
      {rightLink && <button type="button" className="text-link" onClick={rightLinkClick}>{rightLink}</button>}
    </div>
    <div className="input-wrap">
      <input id={inputId} type={type} value={value} onChange={onChange}
        placeholder={placeholder} autoComplete={autoComplete} inputMode={inputMode}
        maxLength={maxLength} disabled={disabled} />
      {onToggleVisible && <button type="button" className="visibility-toggle"
        onClick={onToggleVisible} aria-label={toggleVisible ? 'Скрыть пароль' : 'Показать пароль'}
        title={toggleVisible ? 'Скрыть пароль' : 'Показать пароль'}>
        <span aria-hidden="true">икон<br/>ка</span>
      </button>}
    </div>
  </div>;
}

export function ActionButton({ children, type = 'button', variant = 'primary', size = 'wide', disabled, onClick }) {
  return <button className={`action-button action-button--${variant} action-button--${size}`}
    type={type} disabled={disabled} onClick={onClick}>{children}</button>;
}

export function CenterActions({ children, className = '' }) {
  return <div className={`center-actions ${className}`}>{children}</div>;
}
