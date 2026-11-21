import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './TripPage.css';

const demoMembers = [
  { id: 1, name: 'Алексей', virtual: false },
  { id: 2, name: 'Елена', virtual: false },
  { id: 3, name: 'Игорь', virtual: false },
  { id: 4, name: 'Марина', virtual: false },
];

const categories = [
  { value: 'Food', title: 'Еда' },
  { value: 'Transport', title: 'Транспорт' },
  { value: 'Accommodation', title: 'Жильё' },
  { value: 'Entertainment', title: 'Развлечения' },
  { value: 'Other', title: 'Другое' },
];

const demoExpenses = [
  { id: 1, title: 'Продукты', category: 'Food', amount: 1750, paidByMemberId: 2, date: '23 ноября, 19:20', receiptName: 'Чек.jpg' },
  { id: 2, title: 'Продукты', category: 'Food', amount: 1750, paidByMemberId: 2, date: '23 ноября, 19:20', receiptName: 'Чек.jpg' },
  { id: 3, title: 'Продукты', category: 'Food', amount: 1750, paidByMemberId: 2, date: '23 ноября, 19:20', receiptName: 'Чек.jpg' },
  { id: 4, title: 'Продукты', category: 'Food', amount: 1750, paidByMemberId: 2, date: '23 ноября, 19:20', receiptName: 'Чек.jpg' },
  { id: 5, title: 'Продукты', category: 'Food', amount: 1750, paidByMemberId: 2, date: '23 ноября, 19:20', receiptName: 'Чек.jpg' },
];

const demoDebts = [
  { id: 1, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: false },
  { id: 2, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: false },
  { id: 3, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: true },
  { id: 4, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: true },
  { id: 5, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: true },
  { id: 6, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: true },
  { id: 7, fromMemberId: 1, toMemberId: 2, amount: 1500, settled: true },
];

const tabs = [
  { id: 'expenses', title: 'Расходы' },
  { id: 'debts', title: 'Долги' },
];

function initialTrip() {
  return {
    members: demoMembers.map((item) => ({ ...item })),
    expenses: demoExpenses.map((item) => ({ ...item })),
    debts: demoDebts.map((item) => ({ ...item })),
  };
}

function readTrip(tripId) {
  try {
    const raw = localStorage.getItem(`ttravel_trip_demo_v2_${tripId}`);
    if (raw) {
      const saved = JSON.parse(raw);
      if (Array.isArray(saved.members) && Array.isArray(saved.expenses) &&
          Array.isArray(saved.debts)) {
        return saved;
      }
    }
  } catch {}
  return initialTrip();
}

function money(value) {
  return `${Number(value || 0).toLocaleString('ru-RU', { maximumFractionDigits: 2 })} ₽`;
}

function toCents(value) {
  const amount = Number(String(value).replace(',', '.'));
  return Number.isFinite(amount) && amount >= 0 ? Math.round(amount * 100) : -1;
}

function nameOf(members, id) {
  return members.find((item) => item.id === Number(id))?.name ?? 'Участник';
}

function initials(name) {
  return String(name || '?').trim().slice(0, 1).toUpperCase();
}

function nowLabel() {
  return new Date().toLocaleString('ru-RU', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });
}

function NewExpenseModal({ members, onClose, onSave }) {
  const [title, setTitle] = useState('');
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState('Food');
  const [payer, setPayer] = useState(String(members[0]?.id ?? '1'));
  const [mode, setMode] = useState('equal');
  const [selected, setSelected] = useState(members.map((m) => m.id));
  const [manualShares, setManualShares] = useState({});
  const [receipt, setReceipt] = useState(null);
  const [error, setError] = useState('');

  const totalCents = toCents(amount);
  const shares = useMemo(() => {
    if (totalCents <= 0 || selected.length === 0) return {};
    if (mode === 'manual') {
      return Object.fromEntries(selected.map((id) => [id, toCents(manualShares[id] ?? '')]));
    }
    const base = Math.floor(totalCents / selected.length);
    const remainder = totalCents % selected.length;
    return Object.fromEntries(selected.map((id, index) => [id, base + (index < remainder ? 1 : 0)]));
  }, [totalCents, mode, manualShares, selected]);
  const allocatedCents = Object.values(shares).reduce((sum, cents) => sum + Math.max(0, cents), 0);

  useEffect(() => {
    function onEscape(event) { if (event.key === 'Escape') onClose(); }
    window.addEventListener('keydown', onEscape);
    return () => window.removeEventListener('keydown', onEscape);
  }, [onClose]);

  function submit(event) {
    event.preventDefault();
    if (!title.trim()) return setError('Введите название расхода.');
    if (!Number.isFinite(totalCents) || totalCents <= 0) return setError('Введите сумму больше нуля.');
    if (!selected.length) return setError('Выберите хотя бы одного участника.');
    if (mode === 'manual' && (Object.values(shares).some((v) => v < 0) || allocatedCents !== totalCents)) {
      return setError('Сумма долей должна точно совпадать с суммой расхода.');
    }
    onSave({
      title: title.trim(),
      amount: totalCents / 100,
      category,
      paidByMemberId: Number(payer),
      date: nowLabel(),
      receiptName: receipt?.name ?? '',
      splits: selected.map((memberId) => ({ memberId, shareAmount: shares[memberId] / 100 })),
    });
  }

  return (
    <div className="tp-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
      <section className="tp-modal" role="dialog" aria-modal="true" aria-labelledby="tp-expense-title">
        <div className="tp-modal-head">
          <div><h2 id="tp-expense-title">Добавить расход</h2><p>Название поездки</p></div>
          <button className="tp-close-button" type="button" onClick={onClose} aria-label="Закрыть">Закрыть</button>
        </div>
        <form onSubmit={submit} className="tp-form">
          <button className="tp-ocr" type="button" disabled title="Распознавание чека будет доступно после подключения API">
            Распознать чек через ИИ <span>после подключения API</span>
          </button>
          <label className="tp-field">Название расхода
            <input value={title} onChange={(e) => { setTitle(e.target.value); setError(''); }} placeholder="Например, ужин в кафе" maxLength={100} required />
          </label>
          <div className="tp-form-grid">
            <label className="tp-field">Сумма, ₽
              <input type="number" min="0.01" step="0.01" value={amount} onChange={(e) => { setAmount(e.target.value); setError(''); }} placeholder="3000" required />
            </label>
            <label className="tp-field">Категория
              <select value={category} onChange={(e) => setCategory(e.target.value)}>
                {categories.map((item) => <option key={item.value} value={item.value}>{item.title}</option>)}
              </select>
            </label>
          </div>
          <label className="tp-field">Кто платил
            <select value={payer} onChange={(e) => setPayer(e.target.value)}>
              {members.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>
          <div className="tp-splits-head"><strong>За кого</strong><span>Как разделить сумму?</span></div>
          <div className="tp-mode-switch" role="group" aria-label="Способ распределения суммы">
            <button className={mode === 'equal' ? 'is-active' : ''} type="button" onClick={() => { setMode('equal'); setError(''); }}>Поровну</button>
            <button className={mode === 'manual' ? 'is-active' : ''} type="button" onClick={() => { setMode('manual'); setError(''); }}>Своя сумма</button>
          </div>
          <div className="tp-split-list">
            {members.map((item) => {
              const checked = selected.includes(item.id);
              return (
                <div className="tp-split-row" key={item.id}>
                  <label className="tp-check"><input type="checkbox" checked={checked} onChange={(event) => {
                    setSelected((old) => event.target.checked ? [...old, item.id] : old.filter((id) => id !== item.id));
                    setError('');
                  }} /> <span className="tp-avatar tp-avatar--sm">{initials(item.name)}</span> {item.name}</label>
                  {checked ? mode === 'equal' ? <strong>{money((shares[item.id] ?? 0) / 100)}</strong> :
                    <input aria-label={`Доля: ${item.name}`} className="tp-share-input" type="number" min="0" step="0.01" placeholder="0" value={manualShares[item.id] ?? ''}
                      onChange={(event) => { setManualShares((old) => ({ ...old, [item.id]: event.target.value })); setError(''); }} /> :
                    <span className="tp-muted">Не участвует</span>}
                </div>
              );
            })}
          </div>
          <div className={`tp-allocated ${mode === 'manual' && totalCents > 0 && allocatedCents !== totalCents ? 'tp-allocated--warn' : ''}`}>
            <span>Распределено {mode === 'equal' ? 'поровну' : 'вручную'}</span>
            <strong>{money(allocatedCents / 100)} / {money(Math.max(totalCents, 0) / 100)}</strong>
          </div>
          <label className="tp-upload">{receipt ? receipt.name : 'Прикрепить фото чека'}
            <input type="file" accept="image/*" onChange={(event) => setReceipt(event.target.files?.[0] || null)} />
          </label>
          {error && <p className="tp-form-error" role="alert">{error}</p>}
          <div className="tp-modal-actions"><button type="button" className="tp-button tp-button--light" onClick={onClose}>Отмена</button><button className="tp-button tp-button--primary" type="submit">Добавить расход</button></div>
        </form>
      </section>
    </div>
  );
}

function AddMemberModal({ onClose, onSave }) {
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  useEffect(() => {
    function onEscape(event) { if (event.key === 'Escape') onClose(); }
    window.addEventListener('keydown', onEscape);
    return () => window.removeEventListener('keydown', onEscape);
  }, [onClose]);
  function submit(event) {
    event.preventDefault();
    if (!name.trim()) return setError('Введите имя участника.');
    onSave(name.trim());
  }
  return <div className="tp-overlay" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}>
    <section className="tp-modal tp-modal--small" role="dialog" aria-modal="true" aria-labelledby="tp-add-member-title">
      <div className="tp-modal-head"><div><h2 id="tp-add-member-title">Добавить участника</h2><p>Демонстрационный виртуальный участник</p></div><button className="tp-close-button" onClick={onClose} aria-label="Закрыть" type="button">Закрыть</button></div>
      <form className="tp-form" onSubmit={submit}>
        <label className="tp-field">Имя участника<input autoFocus value={name} onChange={(e) => { setName(e.target.value); setError(''); }} placeholder="Например, Андрей" maxLength={60}/></label>
        {error && <p className="tp-form-error">{error}</p>}
        <div className="tp-modal-actions"><button className="tp-button tp-button--light" type="button" onClick={onClose}>Отмена</button><button className="tp-button tp-button--primary" type="submit">Добавить</button></div>
      </form>
    </section>
  </div>;
}

function TripWorkspace({ tripId }) {
  const navigate = useNavigate();
  const [data, setData] = useState(() => readTrip(tripId));
  const [tab, setTab] = useState('expenses');
  const [modal, setModal] = useState(null);
  const [notice, setNotice] = useState('');
  const userName = (() => { try { return sessionStorage.getItem('ttravel_user_name') || 'Алексей'; } catch { return 'Алексей'; } })();
  const total = 24000 + data.expenses.filter((item) => item.id > 5).reduce((sum, item) => sum + Number(item.amount || 0), 0);
  const remaining = 24000 - data.debts.filter((item) => item.settled && item.id <= 2).reduce((sum, item) => sum + Number(item.amount || 0), 0);

  useEffect(() => {
    try { localStorage.setItem(`ttravel_trip_demo_v2_${tripId}`, JSON.stringify(data)); } catch {}
  }, [tripId, data]);

  function addExpense(expense) {
    setData((old) => ({
      ...old,
      expenses: [{ ...expense, id: Date.now() }, ...old.expenses],
    }));
    setModal(null); setNotice('Расход добавлен в демонстрационные данные.'); setTab('expenses');
  }
  function addMember(name) {
    if (data.members.some((m) => m.name.toLowerCase() === name.toLowerCase())) {
      setNotice('Участник с таким именем уже есть.'); setModal(null); return;
    }
    setData((old) => ({
      ...old,
      members: [...old.members, { id: Math.max(...old.members.map((m) => m.id), 0) + 1, name, virtual: true }],
    }));
    setModal(null); setNotice('Виртуальный участник добавлен.');
  }
  function settleDebt(id) {
    const target = data.debts.find((d) => d.id === id);
    if (!target || target.settled) return;
    setData((old) => ({
      ...old,
      debts: old.debts.map((d) => d.id === id ? { ...d, settled: true } : d),
    }));
    setNotice('Долг отмечен как погашенный в деморежиме.');
  }

  return (
    <div className="tp">
      <header className="tp-topbar">
        <div className="tp-topbar-inner">
          <button className="tp-logo" type="button" onClick={() => navigate('/dashboard')} title="Вернуться к поездкам">Т-путешествия</button>
          <div className="tp-user-controls">
            <button type="button" className="tp-theme" aria-label="Переключатель темы" title="Переключить тему" onClick={() => {}}><span /></button>
            <span className="tp-user-name">{userName}</span><span className="tp-avatar">{initials(userName)}</span>
          </div>
        </div>
      </header>
      <main className="tp-main">
        
        <div className="tp-overview">
          <div className="tp-trip-info">
            <div className="tp-titleline"><h1>Название поездки</h1><span className="tp-status">Активная</span></div>
            <div className="tp-meta"><span><span className="tp-placeholder tp-placeholder--tiny" aria-hidden="true"></span>21–30 ноября 2026</span><span><span className="tp-placeholder tp-placeholder--tiny" aria-hidden="true"></span>{data.members.length} участника</span></div>
            <div className="tp-members"><div className="tp-avatar-stack" aria-label="Участники поездки">{data.members.map((m) => <span key={m.id} className="tp-avatar tp-avatar--sm" title={m.name}>{initials(m.name)}</span>)}</div><span>{data.members.map((m) => m.name).join(', ')}</span></div>
            <button type="button" className="tp-add-member" onClick={() => setModal('member')}>Добавить участника</button>
          </div>
          <div className="tp-stats"><div className="tp-stat"><span>Всего потрачено</span><strong>{money(total)}</strong></div><div className="tp-stat"><span>В среднем на человека</span><strong>{money(data.members.length ? total / data.members.length : 0)}</strong></div></div>
        </div>
        <nav className="tp-tabs" aria-label="Разделы карточки поездки">
          {tabs.map((item) => <button key={item.id} type="button" className={`tp-tab ${tab === item.id ? 'is-active' : ''}`} onClick={() => { setTab(item.id); setNotice(''); }} aria-current={tab === item.id ? 'page' : undefined}>{item.title}</button>)}
        </nav>
        {notice && <div className="tp-notice" role="status">{notice}<button type="button" onClick={() => setNotice('')} aria-label="Убрать уведомление">×</button></div>}

        {tab === 'expenses' && <section className="tp-section" aria-labelledby="tp-expenses-heading">
          <div className="tp-section-head"><div><h2 id="tp-expenses-heading">Лента расходов</h2><p>{data.expenses.length} операций</p></div><button className="tp-button tp-button--primary" type="button" onClick={() => setModal('expense')}>Добавить расход</button></div>
          <div className="tp-expenses">
            {data.expenses.length ? data.expenses.map((item) => {
              return <article className="tp-expense" key={item.id}>
                <span className="tp-placeholder tp-placeholder--expense" aria-hidden="true"></span><div className="tp-expense-main"><h3>{item.title}</h3><p>{nameOf(data.members, item.paidByMemberId)} &nbsp; {item.date}</p></div>
                <div className="tp-expense-right"><strong>{money(item.amount)}</strong>{item.receiptName ? <span title={`Прикреплённый чек: ${item.receiptName}`} className="tp-receipt"><span className="tp-placeholder tp-placeholder--tiny" aria-hidden="true"></span>Чек</span> : <span className="tp-muted">Без чека</span>}</div>
              </article>;
            }) : <div className="tp-empty">Расходов пока нет. Добавьте первую трату.</div>}
          </div>
        </section>}

        {tab === 'debts' && <section className="tp-section" aria-labelledby="tp-debts-heading">
          <div className="tp-section-head"><div><h2 id="tp-debts-heading">Кто кому должен</h2><p>{data.debts.filter((d) => !d.settled).length} активных переводов</p></div><div className="tp-debt-total"><strong>{money(remaining)}</strong><span>Осталось вернуть</span></div></div>
          <div className="tp-debts">{data.debts.length ? data.debts.map((debt) => <article className="tp-debt" key={debt.id}>
            <span className="tp-avatar tp-avatar--sm">{initials(nameOf(data.members, debt.fromMemberId))}</span>
            <div className="tp-debt-description"><div className="tp-debt-names"><strong>{nameOf(data.members, debt.fromMemberId)} → {nameOf(data.members, debt.toMemberId)}</strong><span className={debt.settled ? 'tp-settled-label' : ''}>{debt.settled ? 'Возврат подтверждён' : 'Ожидает возврата'}</span></div><strong className="tp-debt-amount">{money(debt.amount)}</strong></div>
            <button className={`tp-button tp-debt-action ${debt.settled ? 'tp-button--light' : 'tp-button--primary'}`} disabled={debt.settled} type="button" onClick={() => settleDebt(debt.id)}>{debt.settled ? 'Погашено' : 'Подтвердить возврат'}</button>
          </article>) : <div className="tp-empty">Задолженностей нет.</div>}</div>
        </section>}

      </main>
      {modal === 'expense' && <NewExpenseModal members={data.members} onClose={() => setModal(null)} onSave={addExpense} />}
      {modal === 'member' && <AddMemberModal onClose={() => setModal(null)} onSave={addMember} />}
    </div>
  );
}

export default function TripPage() {
  const { tripId = '1' } = useParams();
  return <TripWorkspace key={tripId} tripId={tripId} />;
}
