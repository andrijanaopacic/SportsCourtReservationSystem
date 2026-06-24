import { useEffect, useState, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getCourtById,
  getCourtCalendar,
  getTimeSlotsByCourtReservations
} from '../services/api';

function CourtBooking() {
  const { courtId } = useParams();
  const navigate = useNavigate();

  const [court, setCourt] = useState(null);
  const [timeSlots, setTimeSlots] = useState([]);
  const [calendar, setCalendar] = useState([]);

  const [selectedDate, setSelectedDate] = useState(null);
  const [selectedSlots, setSelectedSlots] = useState([]);

  const [currentDate, setCurrentDate] = useState(new Date());

  const [loading, setLoading] = useState(true);
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [error, setError] = useState('');

  const formatDate = (d) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  // Inicijalno učitavanje: podaci o terenu i kalendar za trenutni mesec
  const loadInitialData = async () => {
    setLoading(true);
    setError('');
    try {
      const [courtRes, calendarRes] = await Promise.all([
        getCourtById(courtId),
        getCourtCalendar(courtId, currentDate.getFullYear(), currentDate.getMonth() + 1)
      ]);
      setCourt(courtRes.data);
      setCalendar(calendarRes.data);
    } catch (err) {
      console.error(err);
      const msg = err?.response?.data?.message || 'Failed to load initial data';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  // Učitavanje termina za izabrani datum — bek vraća isAvailable po datumu
  const loadSlotsForDate = useCallback(async (dateStr) => {
    setSlotsLoading(true);
    try {
      const res = await getTimeSlotsByCourtReservations(courtId, dateStr);
      setTimeSlots(res.data);
    } catch (err) {
      console.error('Failed to load time slots for selected date', err);
    } finally {
      setSlotsLoading(false);
    }
  }, [courtId]);

  useEffect(() => {
    loadInitialData();
  }, [courtId, currentDate]);

  useEffect(() => {
    setSelectedSlots([]);
    setTimeSlots([]);
    if (selectedDate) {
      loadSlotsForDate(selectedDate);
    }
  }, [selectedDate, loadSlotsForDate]);

  const toggleSlot = (id) => {
    setSelectedSlots(prev =>
      prev.includes(id)
        ? prev.filter(x => x !== id)
        : [...prev, id]
    );
  };

  const getDayInfo = (dateStr) =>
    calendar.find(x => x.date === dateStr);

  // Ukupna cena — sabira se lokalno samo za UX prikaz pre slanja na bek
  const totalPrice = selectedSlots.reduce((sum, id) => {
    const slot = timeSlots.find(s => s.timeSlotId === id);
    return sum + (slot?.totalPrice || 0);
  }, 0);

  const daysInMonth = new Date(
    currentDate.getFullYear(),
    currentDate.getMonth() + 1,
    0
  ).getDate();

  const firstDay = new Date(
    currentDate.getFullYear(),
    currentDate.getMonth(),
    1
  ).getDay();

  const changeMonth = (offset) => {
    setSelectedDate(null);
    setCurrentDate(
      new Date(
        currentDate.getFullYear(),
        currentDate.getMonth() + offset,
        1
      )
    );
  };

  if (loading && !court) {
    return <div className="page">Loading...</div>;
  }

  return (
    <div className="page">
      {/* HEADER */}
      <div className="page-header">
        <div>
          <div className="page-title">{court?.name}</div>
          <div className="page-subtitle">
            {court?.sportName} • {court?.location}
          </div>
        </div>

        <button className="btn-ghost" onClick={() => navigate('/courts')}>
          ← Back
        </button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {/* CALENDAR */}
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <button onClick={() => changeMonth(-1)}>←</button>
          <h3>
            {currentDate.toLocaleString('default', { month: 'long' })}{' '}
            {currentDate.getFullYear()}
          </h3>
          <button onClick={() => changeMonth(1)}>→</button>
        </div>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(7, 1fr)',
            gap: 6,
            marginTop: 12
          }}
        >
          {['Sun','Mon','Tue','Wed','Thu','Fri','Sat'].map(d => (
            <div key={d} style={{ textAlign: 'center', fontWeight: 600 }}>
              {d}
            </div>
          ))}

          {Array.from({ length: firstDay }).map((_, i) => (
            <div key={i} />
          ))}

          {Array.from({ length: daysInMonth }).map((_, i) => {
            const day = i + 1;
            const date = new Date(
              currentDate.getFullYear(),
              currentDate.getMonth(),
              day
            );

            const dateStr = formatDate(date);
            const dayInfo = getDayInfo(dateStr);
            const count = dayInfo?.reservationCount || 0;
            const isSelected = selectedDate === dateStr;

            return (
              <div
                key={day}
                onClick={() => setSelectedDate(dateStr)}
                style={{
                  padding: 10,
                  borderRadius: 8,
                  cursor: 'pointer',
                  textAlign: 'center',
                  background: isSelected
                    ? '#1565c0'
                    : count === 0
                    ? '#2e7d32'
                    : count < 3
                    ? '#f9a825'
                    : '#c62828',
                  color: '#fff'
                }}
              >
                <div>{day}</div>
                <div style={{ fontSize: 10 }}>{count} booked</div>
              </div>
            );
          })}
        </div>
      </div>

      {/* SLOTS */}
      {selectedDate && (
        <div className="card">
          <h3>Slots for {selectedDate}</h3>

          {slotsLoading ? (
            <p>Loading slots...</p>
          ) : (
            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
              gap: 10
            }}>
              {timeSlots.length === 0 ? (
                <p>Nema definisanih termina za ovaj dan.</p>
              ) : (
                timeSlots.map(slot => {
                  const selected = selectedSlots.includes(slot.timeSlotId);
                  // isAvailable dolazi direktno sa beka — nema duple provere na frontu
                  const isUnavailable = !slot.isAvailable;

                  return (
                    <div
                      key={slot.timeSlotId}
                      onClick={() => !isUnavailable && toggleSlot(slot.timeSlotId)}
                      style={{
                        padding: 16,
                        borderRadius: 10,
                        cursor: isUnavailable ? 'not-allowed' : 'pointer',
                        background: isUnavailable
                          ? '#757575'
                          : selected
                          ? '#1565c0'
                          : '#2e7d32',
                        color: '#fff',
                        textAlign: 'center',
                        opacity: isUnavailable ? 0.6 : 1
                      }}
                    >
                      {slot.startTime.slice(0, 5)} - {slot.endTime.slice(0, 5)}
                      <div>{slot.totalPrice} RSD</div>
                      {isUnavailable && (
                        <div style={{ fontSize: 11, fontWeight: 'bold', marginTop: 4 }}>
                          ZAUZETO
                        </div>
                      )}
                    </div>
                  );
                })
              )}
            </div>
          )}

          {selectedSlots.length > 0 && (
            <div style={{ marginTop: 20 }}>
              <h4>Total: {totalPrice} RSD</h4>

              <button
                className="btn-primary"
                onClick={() => {
                  const items = selectedSlots.map(id => {
                    const slot = timeSlots.find(s => s.timeSlotId === id);
                    return {
                      courtId: Number(courtId),
                      courtName: court.name,
                      date: selectedDate,
                      timeSlotId: id,
                      startTime: slot?.startTime,
                      endTime: slot?.endTime,
                      price: slot?.totalPrice
                    };
                  });

                  // Korpa se prosleđuje kroz router state — bez localStorage
                  navigate('/confirm-reservation', { state: { items } });
                }}
              >
                Continue
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default CourtBooking;