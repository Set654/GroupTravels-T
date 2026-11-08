import { createContext, useContext, useState } from 'react';

const StageDimensions = createContext(254);
const percentX = (v) => `${(v / 550) * 100}%`;
const percentY = (v, fullHeight) => `${(v / fullHeight) * 100}%`;
function rectStyle({ x, y, w, h }, fullHeight) {
  return {
    left: percentX(x), top: percentY(y, fullHeight),
    width: percentX(w), height: percentY(h, fullHeight),
  };
}

export function SvgStage({ name, height, children }) {
  return (
    <StageDimensions.Provider value={height}>
      <section className="svg-stage" style={{ aspectRatio: `550 / ${height}` }}>
        <img className="svg-art" src={`/design/${name}`} alt="" aria-hidden="true" draggable="false" />
        {children}
      </section>
    </StageDimensions.Provider>
  );
}

export function Hotspot({ x, y, w, h, label, onClick, type = 'button', disabled = false }) {
  const fullHeight = useContext(StageDimensions);
  return (
    <button className="svg-hotspot" type={type} aria-label={label}
      style={rectStyle({ x, y, w, h }, fullHeight)}
      onClick={onClick} disabled={disabled} />
  );
}

export function SvgInput({ x, y, w, h = 48, label, value, onChange, type = 'text', maxLength, autoComplete, inputMode, name }) {
  const fullHeight = useContext(StageDimensions);
  const [focused, setFocused] = useState(false);
  return (
    <input
      className={`svg-input${focused || value ? ' has-value' : ''}`}
      style={rectStyle({ x, y, w, h }, fullHeight)}
      name={name} aria-label={label} type={type} value={value} onChange={onChange}
      placeholder=" " onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}
      autoComplete={autoComplete} inputMode={inputMode} maxLength={maxLength}
      spellCheck={false}
    />
  );
}

export function OverlayText({ x, y, w, h, children }) {
  const fullHeight = useContext(StageDimensions);
  return <div className="svg-copy-over" style={rectStyle({ x, y, w, h }, fullHeight)}>{children}</div>;
}

export async function showError(promise) {
  try { await promise; } catch (error) { window.alert(error.message || 'Не удалось выполнить запрос.'); }
}
