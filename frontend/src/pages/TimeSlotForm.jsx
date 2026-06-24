import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getCourts, createTimeSlot, updateTimeSlot, getTimeSlotById } from '../services/api';

function TimePicker({ label, value, onChange }) {
  const hours = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
  const hh = value ? value.split(':')[0] : '08';

  return (
    <div className="form-group">
      <label className="form-label">{label}</label>
      <div style={{ display: 'flex', alignItems: 'center', borderBottom: '1px solid var(--sage)', paddingBottom: '10px' }}>
        {/* Hour selector */}
        <select
          value={hh}
          onChange={e => onChange(`${e.target.value}:00`)}
          style={{
            background: 'transparent', border: 'none', outline: 'none',
            fontFamily: 'Cormorant Garamond, serif', fontSize: '32px',
            fontWeight: 300, color: 'var(--mahogany)',
            cursor: 'pointer', appearance: 'none', width: '56px',
          }}
        >
          {hours.map(h => <option key={h} value={h}>{h}</option>)}
        </select>
        <span style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: '32px',
          color: 'var(--bronze)', lineHeight: 1, userSelect: 'none', margin: '0 2px',
        }}>:</span>
        {/* Minutes */}
        <span style={{
          fontFamily: 'Cormorant Garamond, serif', fontSize: '32px',
          fontWeight: 300, color: 'var(--mahogany)', width: '52px',
          userSelect: 'none',
        }}>00</span>
      </div>
      {/* Quick-select buttons for common full hours */}
      <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap', marginTop: '10px' }}>
        {['06', '08', '10', '12', '14', '16', '18', '20', '22'].map(h => {
          const t = `${h}:00`;
          return (
            <button key={h} type="button" onClick={() => onChange(t)} style={{
              background: value === t ? 'var(--mahogany)' : 'transparent',
              color: value === t ? 'var(--cream)' : 'var(--bronze)',
              border: '1px solid var(--bronze)',
              padding: '4px 10px', fontSize: '10px',
              letterSpacing: '0.08em', cursor: 'pointer',
              transition: 'all 0.15s', fontFamily: 'Inter, sans-serif',
            }}>
              {t}
            </button>
          );
        })}
      </div>
    </div>
  );
}

function TimeSlotForm() {
  const { id } = useParams();
  const navigate = useNavigate();

  const isEdit = id && id !== 'new';

  const [courts, setCourts] = useState([]);
  const [form, setForm] = useState({
    courtId: '',
    date: '',
    startTime: '08:00',
    endTime: '10:00',
    price: '',
    isAvailable: true,
  });
  const [error, setError] = useState('');

  useEffect(() => {
    getCourts({}).then(res => setCourts(res.data)).catch(() => { });
  }, []);

  useEffect(() => {
    if (isEdit) {
      getTimeSlotById(id).then(res => {
        setForm({
          courtId: String(res.data.courtId),
          date: res.data.date,
          startTime: res.data.startTime.slice(0, 5),
          endTime: res.data.endTime.slice(0, 5),
          price: String(res.data.price),
          isAvailable: res.data.isAvailable,
        });
      }).catch(() => setError('Failed to load time slot.'));
    }
  }, [id]);

  const previewDuration = () => {
    if (!form.startTime || !form.endTime) return null;
    const sh = parseInt(form.startTime.split(':')[0]);
    const eh = parseInt(form.endTime.split(':')[0]);
    const hours = eh - sh;
    if (hours <= 0) return null;
    return `${hours}h`;
  };

  const previewTotalPrice = () => {
    const p = parseFloat(form.price);
    if (!p || isNaN(p)) return null;
    const sh = parseInt(form.startTime.split(':')[0]);
    const eh = parseInt(form.endTime.split(':')[0]);
    const hours = eh - sh;
    if (hours <= 0) return null;
    return (p * hours).toLocaleString('sr-RS');
  };

  const handleSubmit = async () => {
    try {
      const data = {
        courtId: parseInt(form.courtId),
        date: form.date,
        startTime: form.startTime + ':00',
        endTime: form.endTime + ':00',
        price: parseFloat(form.price),
        isAvailable: form.isAvailable,
      };
      if (isEdit) {
        await updateTimeSlot(id, data);
      } else {
        await createTimeSlot(data);
      }
      navigate('/timeslots');
    } catch (err) {
      const data = err.response?.data;
      if (Array.isArray(data)) {
        setError(data.map(e => e.errorMessage || e).join(', '));
      } else {
        setError(typeof data === 'string' ? data : 'Something went wrong.');
      }
    }
  };

  const selectedCourt = courts.find(c => c.courtId === parseInt(form.courtId));
  const duration = previewDuration();
  const totalPrice = previewTotalPrice();

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">{isEdit ? 'Edit Time Slot' : 'New Time Slot'}</div>
          <div className="page-subtitle">{isEdit ? 'Update playing time' : 'Schedule a playing time'}</div>
        </div>
        <button className="btn-ghost" onClick={() => navigate('/timeslots')}>← Back</button>
      </div>

      <div style={{ maxWidth: '560px', display: 'flex', flexDirection: 'column', gap: '36px' }}>

        {/* Court selection */}
        <div className="form-group">
          <label className="form-label">Court</label>
          <select
            className="form-select"
            value={form.courtId}
            onChange={e => setForm({ ...form, courtId: e.target.value })}
          >
            <option value="">Select a court...</option>
            {courts.map(c => (
              <option key={c.courtId} value={c.courtId}>{c.name} — {c.location}</option>
            ))}
          </select>
        </div>

        {/* Date picker */}
        <div className="form-group">
          <label className="form-label">Date</label>
          <input
            className="form-input"
            type="date"
            value={form.date}
            onChange={e => setForm({ ...form, date: e.target.value })}
            style={{ fontFamily: 'Inter, sans-serif', fontSize: '15px' }}
          />
        </div>

        {/* Start and end time pickers */}
        <TimePicker label="Start Time" value={form.startTime} onChange={v => setForm({ ...form, startTime: v })} />
        <TimePicker label="End Time" value={form.endTime} onChange={v => setForm({ ...form, endTime: v })} />

        {/* Price per hour input */}
        <div className="form-group">
          <label className="form-label">Price per hour (RSD)</label>
          <input
            className="form-input"
            type="number"
            min="0"
            step="0.01"
            placeholder="e.g. 1500"
            value={form.price}
            onChange={e => setForm({ ...form, price: e.target.value })}
          />
        </div>

        {/* Availability toggle */}
        <div className="form-group">
          <label className="form-label">Availability</label>
          <div style={{ display: 'flex', gap: '12px', marginTop: '6px' }}>
            {[true, false].map(val => (
              <button
                key={String(val)}
                type="button"
                onClick={() => setForm({ ...form, isAvailable: val })}
                style={{
                  background: form.isAvailable === val ? 'var(--mahogany)' : 'transparent',
                  color: form.isAvailable === val ? 'var(--cream)' : 'var(--bronze)',
                  border: '1px solid var(--bronze)',
                  padding: '8px 24px', fontSize: '11px',
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  cursor: 'pointer', transition: 'all 0.15s',
                }}
              >
                {val ? 'Available' : 'Unavailable'}
              </button>
            ))}
          </div>
        </div>

        {/* Preview — shown when court is selected */}
        {selectedCourt && (
          <div style={{ borderTop: '1px solid var(--cream-dark)', borderBottom: '1px solid var(--cream-dark)', padding: '20px 0' }}>
            <div style={{ fontSize: '10px', letterSpacing: '0.15em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '12px' }}>
              Preview
            </div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: '16px', flexWrap: 'wrap' }}>
              <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '20px', color: 'var(--mahogany)' }}>
                {selectedCourt.name}
              </span>
              {form.date && (
                <>
                  <span style={{ color: 'var(--sage)' }}>·</span>
                  <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '18px', color: 'var(--mahogany)' }}>
                    {form.date}
                  </span>
                </>
              )}
              <span style={{ color: 'var(--sage)' }}>·</span>
              <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '20px', color: 'var(--mahogany)' }}>
                {form.startTime} — {form.endTime}
              </span>
              {duration && (
                <>
                  <span style={{ color: 'var(--sage)' }}>·</span>
                  <span style={{ fontSize: '12px', color: 'var(--sage)', letterSpacing: '0.05em' }}>
                    {duration}
                  </span>
                </>
              )}
            </div>
            {/* Price breakdown */}
            {form.price && duration && (
              <div style={{ marginTop: '10px', display: 'flex', gap: '10px', alignItems: 'baseline', flexWrap: 'wrap' }}>
                <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '16px', color: 'var(--sage)' }}>
                  {parseFloat(form.price).toLocaleString('sr-RS')} RSD/h × {duration}
                </span>
                <span style={{ color: 'var(--sage)' }}>=</span>
                <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '22px', color: 'var(--bronze)' }}>
                  {totalPrice} RSD
                </span>
              </div>
            )}
            <div style={{ marginTop: '8px' }}>
              <span style={{
                fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase',
                color: form.isAvailable ? '#4a7c59' : '#991b1b',
                border: `1px solid ${form.isAvailable ? '#4a7c59' : '#991b1b'}`,
                padding: '2px 10px',
              }}>
                {form.isAvailable ? 'Available' : 'Unavailable'}
              </span>
            </div>
          </div>
        )}

        {error && <div className="error-msg">{error}</div>}

        <div style={{ display: 'flex', gap: '16px' }}>
          <button className="btn-primary" onClick={handleSubmit}>
            {isEdit ? 'Save Changes' : 'Save Time Slot'}
          </button>
          <button className="btn-ghost" onClick={() => navigate('/timeslots')}>Cancel</button>
        </div>
      </div>
    </div>
  );
}

export default TimeSlotForm;
