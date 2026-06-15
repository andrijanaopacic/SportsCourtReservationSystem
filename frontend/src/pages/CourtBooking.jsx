import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getCourtById,
  getCourtReservations,
  getTimeSlotsByCourt
} from '../services/api';

function CourtBooking() {
  const { courtId } = useParams();
  const navigate = useNavigate();

  const [court, setCourt] = useState(null);
  const [reservations, setReservations] = useState([]);
  const [timeSlots, setTimeSlots] = useState([]);

  const [selectedDate, setSelectedDate] = useState(
    new Date().toISOString().split('T')[0]
  );

  const [selectedSlots, setSelectedSlots] = useState([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const formatDate = (d) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const loadData = async () => {
    setLoading(true);
    setError('');

    try {
      const today = new Date();

      const startDate = formatDate(
        new Date(today.getFullYear(), today.getMonth(), 1)
      );

      const endDate = formatDate(
        new Date(today.getFullYear(), today.getMonth() + 1, 0)
      );

      const [courtRes, slotsRes, reservationsRes] = await Promise.all([
        getCourtById(courtId),
        getTimeSlotsByCourt(courtId),
        getCourtReservations(courtId, { startDate, endDate })
      ]);

      setCourt(courtRes.data);
      setTimeSlots(slotsRes.data);
      setReservations(reservationsRes.data);
    } catch (err) {
      console.error(err);
      setError('Failed to load data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [courtId]);

  // 🔥 BITNO: reset selection na promenu datuma + reload rezervacija
  useEffect(() => {
    setSelectedSlots([]);
    loadData();
  }, [selectedDate]);

  const handleDateChange = (e) => {
    setSelectedDate(e.target.value);
    setSuccess('');
    setError('');
  };

  const isSlotTaken = (slotId) => {
    return reservations.some(r =>
      r.items?.some(i =>
        String(i.date).substring(0, 10) === selectedDate &&
        i.timeSlotId === slotId
      )
    );
  };

  const toggleSlot = (slotId) => {
    setSelectedSlots(prev =>
      prev.includes(slotId)
        ? prev.filter(id => id !== slotId)
        : [...prev, slotId]
    );
  };

  const totalPrice = selectedSlots.reduce((sum, slotId) => {
    const slot = timeSlots.find(s => s.timeSlotId === slotId);
    return sum + (slot?.price ?? 0);
  }, 0);

  if (loading && !court) {
    return (
      <div className="page">
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <div className="page">

      <div className="page-header">
        <div>
          <div className="page-title">{court?.name}</div>
          <div className="page-subtitle">
            {court?.sportName} • {court?.location}
          </div>
        </div>

        <button
          className="btn-ghost"
          onClick={() => navigate('/courts')}
        >
          ← Back
        </button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {/* DATE */}
      <div className="card" style={{ marginBottom: '24px' }}>
        <label className="form-label">Select Date</label>

        <input
          type="date"
          className="form-input"
          value={selectedDate}
          min={new Date().toISOString().split('T')[0]}
          onChange={handleDateChange}
          style={{ maxWidth: '240px' }}
        />
      </div>

      {/* SLOTS */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
          gap: '12px'
        }}
      >
        {timeSlots.map(slot => {
          const taken = isSlotTaken(slot.timeSlotId);
          const selected = selectedSlots.includes(slot.timeSlotId);

          return (
            <div
              key={slot.timeSlotId}
              onClick={() => !taken && toggleSlot(slot.timeSlotId)}
              style={{
                padding: '18px',
                borderRadius: '10px',
                cursor: taken ? 'not-allowed' : 'pointer',
                background: taken
                  ? '#c62828'
                  : selected
                  ? '#1565c0'
                  : '#2e7d32',
                color: '#fff',
                textAlign: 'center',
                border: selected
                  ? '3px solid #0d47a1'
                  : '3px solid transparent',
                userSelect: 'none'
              }}
            >
              <div style={{ fontWeight: 'bold' }}>
                {slot.startTime} - {slot.endTime}
              </div>

              <div style={{ fontSize: '12px', marginTop: '6px' }}>
                {taken ? 'RESERVED' : selected ? 'SELECTED' : 'AVAILABLE'}
              </div>

              <div style={{ marginTop: '8px' }}>
                {slot.price} RSD
              </div>
            </div>
          );
        })}
      </div>

      {/* SUMMARY (NO POST HERE) */}
      {selectedSlots.length > 0 && (
        <div className="card" style={{ marginTop: '24px' }}>
          <h3>Selected Slots</h3>

          <ul>
            {selectedSlots.map(id => {
              const slot = timeSlots.find(s => s.timeSlotId === id);
              return (
                <li key={id}>
                  {slot?.startTime} - {slot?.endTime} ({slot?.price} RSD)
                </li>
              );
            })}
          </ul>

          <div style={{ marginTop: '10px', fontWeight: 'bold' }}>
            Total: {totalPrice} RSD
          </div>

          {/* 🔥 IDE U CONFIRM PAGE */}
          <button
            className="btn-primary"
            style={{ marginTop: '12px' }}
            onClick={() => {
              const cart = JSON.parse(localStorage.getItem('reservationCart')) || [];

              const newItems = selectedSlots.map(slotId => {
                const slot = timeSlots.find(s => s.timeSlotId === slotId);

                return {
                  courtId: Number(courtId),
                  courtName: court.name,
                  date: selectedDate,
                  timeSlotId: slotId,
                  startTime: slot?.startTime,
                  endTime: slot?.endTime,
                  price: slot?.price
                };
              });

              localStorage.setItem(
                'reservationCart',
                JSON.stringify([...cart, ...newItems])
              );

              navigate('/confirm-reservation');
            }}
          >
            Add to Reservation
          </button>
        </div>
      )}
    </div>
  );
}

export default CourtBooking;