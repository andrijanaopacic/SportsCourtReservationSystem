import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getCourtById, getCourtReservations } from '../services/api';
import Modal from '../components/Modal';

function CourtCalendar() {
    const { courtId } = useParams();
    const navigate = useNavigate();

    const [court, setCourt] = useState(null);
    const [reservations, setReservations] = useState([]);
    const [selectedDate, setSelectedDate] = useState(null);
    const [currentDate, setCurrentDate] = useState(new Date());
    const [loading, setLoading] = useState(true);
    const [modalMessage, setModalMessage] = useState('');

    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');

        return `${year}-${month}-${day}`;
    };

    const getTimeSlotName = (slotId) => {
        const slots = {
            1: '08:00 - 10:00',
            2: '10:00 - 12:00',
            3: '12:00 - 14:00',
            4: '14:00 - 16:00',
            5: '16:00 - 18:00',
            6: '18:00 - 20:00',
            7: '20:00 - 22:00'
        };

        return slots[slotId] || `Slot ${slotId}`;
    };

    useEffect(() => {
        const loadData = async () => {
            try {
                setLoading(true);

                const startDate = formatDate(
                    new Date(
                        currentDate.getFullYear(),
                        currentDate.getMonth(),
                        1
                    )
                );

                const endDate = formatDate(
                    new Date(
                        currentDate.getFullYear(),
                        currentDate.getMonth() + 1,
                        0
                    )
                );

                const [courtResponse, reservationsResponse] =
                    await Promise.all([
                        getCourtById(courtId),
                        getCourtReservations(courtId, {
                            startDate,
                            endDate
                        })
                    ]);

                setCourt(courtResponse.data);
                setReservations(reservationsResponse.data);
            } catch (error) {
                console.error(error);
                setModalMessage('Failed to load calendar.');
            } finally {
                setLoading(false);
            }
        };

        loadData();
    }, [courtId, currentDate]);

    const getReservationsForDate = (date) => {
        const dateStr = formatDate(date);

        return reservations.filter(reservation =>
            reservation.items?.some(
                item => item.date === dateStr
            )
        );
    };

    const prevMonth = () => {
        setCurrentDate(
            new Date(
                currentDate.getFullYear(),
                currentDate.getMonth() - 1,
                1
            )
        );
    };

    const nextMonth = () => {
        setCurrentDate(
            new Date(
                currentDate.getFullYear(),
                currentDate.getMonth() + 1,
                1
            )
        );
    };

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

    const monthNames = [
        'January',
        'February',
        'March',
        'April',
        'May',
        'Jun',
        'July',
        'August',
        'September',
        'October',
        'November',
        'December'
    ];

    const dayNames = [
        'Sun',
        'Mon',
        'Tue',
        'Wed',
        'Thur',
        'Fri',
        'Sat'
    ];

    const selectedReservations = selectedDate
        ? getReservationsForDate(selectedDate)
        : [];

    if (loading && !court) {
        return (
            <div className="page">
                <p>Loading calendar...</p>
            </div>
        );
    }

    return (
        <div className="page">
            <Modal
                message={modalMessage}
                onClose={() => setModalMessage('')}
            />

            <div className="page-header">
                <div>
                    <div className="page-title">
                        {court?.name || 'Court Calendar'}
                    </div>

                    <div className="page-subtitle">
                        {court?.sportName} • {court?.location}
                    </div>
                </div>

                <button
                    className="btn-ghost"
                    onClick={() => navigate('/reservations/create')}
                >
                    ← Back to Courts
                </button>
            </div>

            <div className="card">
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        gap: '16px',
                        marginBottom: '24px'
                    }}
                >
                    <button
                        className="btn-ghost"
                        onClick={prevMonth}
                    >
                        ← Prev
                    </button>

                    <h2>
                        {monthNames[currentDate.getMonth()]}{' '}
                        {currentDate.getFullYear()}
                    </h2>

                    <button
                        className="btn-ghost"
                        onClick={nextMonth}
                    >
                        Next →
                    </button>
                </div>

                <div
                    style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(7, 1fr)',
                        gap: '8px'
                    }}
                >
                    {dayNames.map(day => (
                        <div
                            key={day}
                            style={{
                                textAlign: 'center',
                                fontWeight: 'bold',
                                padding: '8px'
                            }}
                        >
                            {day}
                        </div>
                    ))}

                    {Array.from({ length: firstDay }).map((_, i) => (
                        <div key={`empty-${i}`}></div>
                    ))}

                    {Array.from({ length: daysInMonth }).map((_, index) => {
                        const day = index + 1;

                        const date = new Date(
                            currentDate.getFullYear(),
                            currentDate.getMonth(),
                            day
                        );

                        const reservationsForDay =
                            getReservationsForDate(date);

                        const isSelected =
                            selectedDate &&
                            selectedDate.toDateString() ===
                                date.toDateString();

                        return (
                            <div
                                key={day}
                                onClick={() =>
                                    setSelectedDate(date)
                                }
                                style={{
                                    minHeight: '80px',
                                    border: '1px solid #ddd',
                                    borderRadius: '8px',
                                    padding: '8px',
                                    cursor: 'pointer',
                                    background: isSelected
                                        ? '#b08d57'
                                        : '#fff',
                                    color: isSelected
                                        ? '#fff'
                                        : '#000'
                                }}
                            >
                                <div>{day}</div>

                                {reservationsForDay.length > 0 && (() => {
                                    const dateStr = formatDate(date);
                                    const slotCount = reservationsForDay.reduce((sum, r) =>
                                        sum + (r.items?.filter(i => {
                                            const d = typeof i.date === 'string' ? i.date.split('T')[0] : String(i.date);
                                            return d === dateStr;
                                        }).length ?? 0), 0);
                                    return (
                                        <div style={{ fontSize: '12px', marginTop: '6px' }}>
                                            {slotCount} {slotCount === 1 ? 'appointment' : 'appointments'}
                                        </div>
                                    );
                                })()}
                            </div>
                        );
                    })}
                </div>
            </div>

            {selectedDate && (
                <div
                    className="card"
                    style={{ marginTop: '24px' }}
                >
                    <h3>
                        Reservations for{' '}
                        {selectedDate.toLocaleDateString('sr-RS')}
                    </h3>

                    {selectedReservations.length === 0 ? (
                        <p>No reservations.</p>
                    ) : (
                        selectedReservations.map(reservation => (
                            <div
                                key={reservation.reservationId}
                                style={{
                                    border: '1px solid #ddd',
                                    borderRadius: '8px',
                                    padding: '16px',
                                    marginBottom: '12px'
                                }}
                                    >

                                <div style={{ marginTop: '10px' }}>
                                    {reservation.items
                                        .filter(
                                            item =>
                                                item.date ===
                                                formatDate(
                                                    selectedDate
                                                )
                                        )
                                        .map(item => (
                                            <div
                                                key={item.rowNumber}
                                            >
                                                {getTimeSlotName(
                                                    item.timeSlotId
                                                )}
                                               
                                            </div>
                                        ))}
                                </div>

                                <div
                                    style={{
                                        marginTop: '10px',
                                        fontWeight: 'bold'
                                    }}
                                >
                                </div>
                            </div>
                        ))
                    )}
                </div>
            )}
        </div>
    );
}

export default CourtCalendar;

