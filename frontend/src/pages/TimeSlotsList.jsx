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

  const handleDelete = async (id) => {
    try {
      await deleteTimeSlot(id);
      fetchSlots();
    } catch (err) {
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

  // Group slots by court for display
  const grouped = slots.reduce((acc, slot) => {
    const key = slot.courtId;
    if (!acc[key]) acc[key] = { courtName: slot.courtName, slots: [] };
    acc[key].slots.push(slot);
    return acc;
  }, {});

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      <div className="page-header">
        <div>
          <div className="page-title">Time Slots</div>
          <div className="page-subtitle">Available playing times by court</div>
        </div>
        {/* Add button — only shown to Admin */}
        {isAdmin() && (
          <Link to="/timeslots/new">
            <button className="btn-primary">+ Add Time Slot</button>
          </Link>
        )}
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', alignItems: 'flex-end', marginBottom: '36px' }}>

        {/* Court filter */}
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

        {/* Availability filter */}
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

        {/* Min price filter */}
        <div className="form-group">
          <label className="form-label">Min Price (RSD)</label>
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

        {/* Max price filter */}
        <div className="form-group">
          <label className="form-label">Max Price (RSD)</label>
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

        {/* Reset filters */}
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
        // Single court selected 
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '12px' }}>
          {slots.map(slot => (
            <SlotCard key={slot.timeSlotId} slot={slot} onDelete={handleDelete} formatTime={formatTime} formatDuration={formatDuration} admin={isAdmin()} />
          ))}
        </div>
      ) : (
        // All courts — grouped by court name
        <div style={{ display: 'flex', flexDirection: 'column', gap: '48px' }}>
          {Object.entries(grouped).map(([courtId, { courtName, slots: courtSlots }]) => (
            <div key={courtId}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: '20px' }}>
                <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '22px', fontWeight: 400, color: 'var(--mahogany)' }}>
                  {courtName}
                </div>
                <div style={{ flex: 1, height: '1px', background: 'var(--cream-dark)' }} />
                <div style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--sage)' }}>
                  {courtSlots.length} slot{courtSlots.length !== 1 ? 's' : ''}
                </div>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '12px' }}>
                {courtSlots.map(slot => (
                  <SlotCard key={slot.timeSlotId} slot={slot} onDelete={handleDelete} formatTime={formatTime} formatDuration={formatDuration} admin={isAdmin()} />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
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
        border: `1px solid ${hovered ? 'var(--bronze)' : slot.isAvailable ? 'var(--cream-dark)' : 'rgba(153,27,27,0.2)'}`,
        padding: '20px',
        transition: 'border-color 0.2s, background 0.2s',
        background: hovered ? 'var(--cream-dark)' : 'var(--cream)',
        display: 'flex', flexDirection: 'column', gap: '6px',
        opacity: slot.isAvailable ? 1 : 0.65,
      }}
    >
      <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '26px', fontWeight: 300, color: 'var(--mahogany)', lineHeight: 1 }}>
        {formatTime(slot.startTime)}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <span style={{ width: '16px', height: '1px', background: 'var(--sage)', display: 'inline-block' }} />
        <span style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '18px', color: 'var(--bronze)' }}>
          {formatTime(slot.endTime)}
        </span>
      </div>
      {/* Duration */}
      {duration && (
        <div style={{ fontSize: '10px', color: 'var(--sage)', letterSpacing: '0.08em', textTransform: 'uppercase', marginTop: '2px' }}>
          {duration}
        </div>
      )}
      {/* Price */}
      <div style={{ fontFamily: 'Cormorant Garamond, serif', fontSize: '16px', color: 'var(--mahogany)', marginTop: '4px' }}>
        {slot.price?.toLocaleString('sr-RS')} RSD
      </div>
      {/* Availability status */}
      <div style={{
        fontSize: '9px', letterSpacing: '0.1em', textTransform: 'uppercase',
        color: slot.isAvailable ? '#4a7c59' : '#991b1b',
        marginTop: '2px',
      }}>
        {slot.isAvailable ? '● Available' : '● Unavailable'}
      </div>

      {/* Edit and Delete — visible on hover, only for Admin */}
      {hovered && admin && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '10px', alignItems: 'center' }}>
          <Link to={`/timeslots/${slot.timeSlotId}`}>
            <button className="btn-ghost" style={{ padding: '5px 14px', fontSize: '10px' }}>Edit</button>
          </Link>
          <span style={{ color: 'var(--sage)' }}>|</span>
          <button
            className="btn-danger"
            onClick={() => onDelete(slot.timeSlotId)}
            style={{ fontSize: '10px', letterSpacing: '0.1em' }}
          >
            Delete
          </button>
        </div>
      )}
    </div>
  );
}

export default TimeSlotsList;
