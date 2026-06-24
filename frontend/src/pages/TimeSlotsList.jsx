import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getTimeSlots, getTimeSlotsByCourt, getCourts, deleteTimeSlot } from '../services/api';
import Modal from '../components/Modal';
import { useAuth } from '../context/AuthContext';

function TimeSlotsList() {
  const { isAdmin } = useAuth();
  const [slots, setSlots] = useState([]);
  const [courts, setCourts] = useState([]);
  const [filterCourtId, setFilterCourtId] = useState('');
  const [filterAvailable, setFilterAvailable] = useState('');
  const [filterMinPrice, setFilterMinPrice] = useState('');
  const [filterMaxPrice, setFilterMaxPrice] = useState('');
  const [loading, setLoading] = useState(true);
  const [modalMessage, setModalMessage] = useState('');
  const [confirmId, setConfirmId] = useState(null);

  useEffect(() => {
    getCourts({}).then(res => setCourts(res.data)).catch(() => { });
  }, []);

  useEffect(() => {
    fetchSlots();
  }, [filterCourtId, filterAvailable, filterMinPrice, filterMaxPrice]);

  const fetchSlots = async () => {
    setLoading(true);
    try {
      let res;
      if (filterCourtId) {
        res = await getTimeSlotsByCourt(filterCourtId);
      } else {
        res = await getTimeSlots({
          isAvailable: filterAvailable !== '' ? filterAvailable === 'true' : undefined,
          minPrice: filterMinPrice !== '' ? parseFloat(filterMinPrice) : undefined,
          maxPrice: filterMaxPrice !== '' ? parseFloat(filterMaxPrice) : undefined,
        });
      }
      setSlots(res.data);
    } catch {
      setModalMessage('Failed to load time slots.');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = (id) => {
    setConfirmId(id);
  };

  const handleConfirmDelete = async () => {
    try {
      await deleteTimeSlot(confirmId);
      setConfirmId(null);
      fetchSlots();
    } catch (err) {
      setConfirmId(null);
      setModalMessage(err.response?.data || 'Cannot delete time slot.');
    }
  };

  const handleReset = () => {
    setFilterCourtId('');
    setFilterAvailable('');
    setFilterMinPrice('');
    setFilterMaxPrice('');
  };

  const formatTime = (t) => (typeof t === 'string' ? t.slice(0, 5) : t);

  const formatDuration = (duration) => {
    if (!duration) return null;
    const parts = duration.split(':');
    const h = parseInt(parts[0]);
    const m = parseInt(parts[1]);
    if (h > 0 && m > 0) return `${h}h ${m}min`;
    if (h > 0) return `${h}h`;
    return `${m}min`;
  };

  const grouped = slots.reduce((acc, slot) => {
    const courtKey = slot.courtId;
    if (!acc[courtKey]) acc[courtKey] = { courtName: slot.courtName, byDate: {} };
    const dateKey = slot.date || '—';
    if (!acc[courtKey].byDate[dateKey]) acc[courtKey].byDate[dateKey] = [];
    acc[courtKey].byDate[dateKey].push(slot);
    return acc;
  }, {});

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />
      <Modal
        message={confirmId ? 'Are you sure you want to delete this time slot? This action cannot be undone.' : ''}
        onClose={() => setConfirmId(null)}
        onConfirm={handleConfirmDelete}
        confirmText="Delete"
        cancelText="Cancel"
      />

      <div className="page-header">
        <div>
          <div className="page-title">Time Slots</div>
          <div className="page-subtitle">Available playing times by court</div>
        </div>
        {isAdmin() && (
          <Link to="/timeslots/new">
            <button className="btn-primary">+ Add Time Slot</button>
          </Link>
        )}
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', alignItems: 'flex-end', marginBottom: '36px' }}>

        <div className="form-group">
          <label className="form-label">Court</label>
          <select
            className="form-select"
            value={filterCourtId}
            onChange={e => setFilterCourtId(e.target.value)}
            style={{ width: '200px' }}
          >
            <option value="">All courts</option>
            {courts.map(c => (
              <option key={c.courtId} value={c.courtId}>{c.name}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label className="form-label">Availability</label>
          <div style={{ display: 'flex', gap: '8px' }}>
            {[
              { val: '', label: 'All' },
              { val: 'true', label: 'Available' },
              { val: 'false', label: 'Unavailable' },
            ].map(({ val, label }) => (
              <button
                key={val}
                onClick={() => setFilterAvailable(val)}
                style={{
                  background: filterAvailable === val ? 'var(--mahogany)' : 'transparent',
                  color: filterAvailable === val ? 'var(--cream)' : 'var(--bronze)',
                  border: '1px solid var(--bronze)',
                  padding: '6px 14px', fontSize: '10px',
                  letterSpacing: '0.08em', textTransform: 'uppercase',
                  cursor: 'pointer', transition: 'all 0.15s',
                }}
              >
                {label}
              </button>
            ))}
          </div>
        </div>

        <div className="form-group">
          <label className="form-label">Min Price/h (RSD)</label>
          <input
            className="form-input"
            type="number"
            min="0"
            placeholder="e.g. 500"
            value={filterMinPrice}
            onChange={e => setFilterMinPrice(e.target.value)}
            style={{ width: '130px' }}
          />
        </div>

        <div className="form-group">
          <label className="form-label">Max Price/h (RSD)</label>
          <input
            className="form-input"
            type="number"
            min="0"
            placeholder="e.g. 3000"
            value={filterMaxPrice}
            onChange={e => setFilterMaxPrice(e.target.value)}
            style={{ width: '130px' }}
          />
        </div>

        <button className="btn-ghost" onClick={handleReset} style={{ marginBottom: '2px' }}>
          Reset
        </button>
      </div>

      {loading ? (
        <div style={{ color: 'var(--sage)', padding: '48px 0', textAlign: 'center', fontSize: '12px', letterSpacing: '0.12em', textTransform: 'uppercase' }}>
          Loading...
        </div>
      ) : slots.length === 0 ? (
        <div style={{
          padding: '64px 0', textAlign: 'center',
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: '24px', fontWeight: 300, fontStyle: 'italic',
          color: 'var(--sage)',
        }}>
          No time slots found.
        </div>
      ) : filterCourtId ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '32px' }}>
          {Object.entries(
            slots.reduce((acc, slot) => {
              const dk = slot.date || '—';
              if (!acc[dk]) acc[dk] = [];
              acc[dk].push(slot);
              return acc;
            }, {})
          ).sort(([a], [b]) => a.localeCompare(b)).map(([date, dateSlots]) => (
            <div key={date}>
              <DateHeader date={date} count={dateSlots.length} />
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '12px' }}>
                {dateSlots.map(slot => (
                  <SlotCard key={slot.timeSlotId} slot={slot} onDelete={handleDelete} formatTime={formatTime} formatDuration={formatDuration} admin={isAdmin()} />
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '56px' }}>
          {Object.entries(grouped).map(([courtId, { courtName, byDate }]) => (
            <div key={courtId}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: '24px' }}>
                <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '22px', fontWeight: 400, color: 'var(--mahogany)' }}>
                  {courtName}
                </div>
                <div style={{ flex: 1, height: '1px', background: 'var(--cream-dark)' }} />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
                {Object.entries(byDate).sort(([a], [b]) => a.localeCompare(b)).map(([date, dateSlots]) => (
                  <div key={date}>
                    <DateHeader date={date} count={dateSlots.length} />
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '12px' }}>
                      {dateSlots.map(slot => (
                        <SlotCard key={slot.timeSlotId} slot={slot} onDelete={handleDelete} formatTime={formatTime} formatDuration={formatDuration} admin={isAdmin()} />
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function DateHeader({ date, count }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '12px' }}>
      <div style={{ fontSize: '11px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)' }}>
        {date}
      </div>
      <div style={{ flex: 1, height: '1px', background: 'var(--cream-dark)' }} />
      <div style={{ fontSize: '9px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--sage)' }}>
        {count} slot{count !== 1 ? 's' : ''}
      </div>
    </div>
  );
}

function SlotCard({ slot, onDelete, formatTime, formatDuration, admin }) {
  const [hovered, setHovered] = useState(false);
  const duration = formatDuration(slot.duration);

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        border: `1px solid ${hovered ? 'var(--bronze)' : slot.isAvailable ? 'var(--cream-dark)' : 'rgba(153,27,27,0.15)'}`,
        background: hovered ? 'var(--cream-dark)' : 'var(--cream)',
        transition: 'all 0.2s',
        opacity: slot.isAvailable ? 1 : 0.7,
        display: 'flex', flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      {/* Top section — time range + status badge */}
      <div style={{
        background: slot.isAvailable ? 'var(--mahogany)' : '#6b2a2a',
        padding: '16px 20px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      }}>
        {/* Time in one row */}
        <div style={{
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: '24px', fontWeight: 300,
          color: 'var(--cream)', letterSpacing: '0.02em',
          display: 'flex', alignItems: 'center', gap: '8px',
        }}>
          <span>{formatTime(slot.startTime)}</span>
          <span style={{ color: 'var(--sage)', fontSize: '16px' }}>→</span>
          <span>{formatTime(slot.endTime)}</span>
        </div>

        {/* Status badge */}
        <span style={{
          fontSize: '9px', letterSpacing: '0.12em', textTransform: 'uppercase',
          color: slot.isAvailable ? '#a8d5b5' : '#f4a0a0',
          border: `1px solid ${slot.isAvailable ? '#a8d5b5' : '#f4a0a0'}`,
          padding: '3px 8px',
        }}>
          {slot.isAvailable ? 'Free' : 'Taken'}
        </span>
      </div>

      {/* Bottom section — details */}
      <div style={{ padding: '16px 20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>

        {/* Duration */}
        {duration && (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{
              fontSize: '9px', letterSpacing: '0.12em', textTransform: 'uppercase',
              color: 'var(--bronze)',
            }}>
              Duration
            </span>
            <span style={{
              fontFamily: 'Cormorant Garamond, serif', fontSize: '16px',
              color: 'var(--mahogany)', fontWeight: 400,
            }}>
              {duration}
            </span>
          </div>
        )}

        {/* Divider */}
        <div style={{ height: '1px', background: 'var(--cream-dark)' }} />

        {/* Price per hour */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <span style={{
            fontSize: '9px', letterSpacing: '0.12em', textTransform: 'uppercase',
            color: 'var(--bronze)',
          }}>
            Price / h
          </span>
          <span style={{
            fontFamily: 'Cormorant Garamond, serif', fontSize: '16px',
            color: 'var(--mahogany)',
          }}>
            {slot.price?.toLocaleString('sr-RS')} RSD
          </span>
        </div>

        {/* Total price — highlighted */}
        {slot.totalPrice != null && (
          <div style={{
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            background: 'var(--cream-dark)', padding: '8px 12px', marginTop: '2px',
          }}>
            <span style={{
              fontSize: '9px', letterSpacing: '0.12em', textTransform: 'uppercase',
              color: 'var(--mahogany)', fontWeight: 500,
            }}>
              Total
            </span>
            <span style={{
              fontFamily: 'Cormorant Garamond, serif', fontSize: '20px',
              color: 'var(--mahogany)', fontWeight: 600,
            }}>
              {slot.totalPrice?.toLocaleString('sr-RS')} RSD
            </span>
          </div>
        )}

        {/* Edit / Delete buttons */}
        {hovered && admin && (
          <div style={{ display: 'flex', gap: '8px', marginTop: '4px', alignItems: 'center' }}>
            <Link to={`/timeslots/${slot.timeSlotId}`} style={{ flex: 1 }}>
              <button className="btn-ghost" style={{ padding: '5px 0', fontSize: '10px', width: '100%' }}>
                Edit
              </button>
            </Link>
            <button
              className="btn-danger"
              onClick={() => onDelete(slot.timeSlotId)}
              style={{ flex: 1, padding: '5px 0', fontSize: '10px', letterSpacing: '0.1em' }}
            >
              Delete
            </button>
          </div>
        )}
      </div>
    </div>
  );
}



export default TimeSlotsList;
