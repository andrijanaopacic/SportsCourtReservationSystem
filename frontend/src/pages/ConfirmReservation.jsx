import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createReservation } from '../services/api';

function ConfirmReservation() {
    const navigate = useNavigate();

    const [items, setItems] = useState([]);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        const cart =
            JSON.parse(localStorage.getItem('reservationCart')) || [];

        setItems(cart);
    }, []);

    const removeItem = (index) => {
        const updated = items.filter((_, i) => i !== index);

        setItems(updated);

        localStorage.setItem(
            'reservationCart',
            JSON.stringify(updated)
        );
    };

    const handleConfirm = async () => {
        if (items.length === 0) return;

        try {
            setSaving(true);
            setError('');

            await createReservation({
                items: items.map(item => ({
                    timeSlotId: item.timeSlotId,
                    date: item.date
                }))
            });

            localStorage.removeItem('reservationCart');

            navigate('/reservations');
        } catch (err) {
            setError(
                err.response?.data ||
                'Failed to create reservation.'
            );
        } finally {
            setSaving(false);
        }
    };

    const totalPrice = items.reduce(
        (sum, item) => sum + item.price,
        0
    );

    return (
        <div className="page">
            <div className="page-header">
                <div>
                    <div className="page-title">
                        Confirm Reservation
                    </div>
                    <div className="page-subtitle">
                        Review selected reservation items
                    </div>
                </div>
            </div>

            {error && (
                <div className="error-msg">
                    {error}
                </div>
            )}

            {items.length === 0 ? (
                <div className="card">
                    No reservation items selected.
                </div>
            ) : (
                <>
                    <table>
                        <thead>
                            <tr>
                                <th>Court</th>
                                <th>Date</th>
                                <th>Time</th>
                                <th>Price</th>
                                <th></th>
                            </tr>
                        </thead>

                        <tbody>
                            {items.map((item, index) => (
                                <tr key={index}>
                                    <td>{item.courtName}</td>

                                    <td>{item.date}</td>

                                    <td>
                                        {item.startTime} - {item.endTime}
                                    </td>

                                    <td>
                                        {item.price} RSD
                                    </td>

                                    <td>
                                        <button
                                            className="btn-danger"
                                            onClick={() =>
                                                removeItem(index)
                                            }
                                        >
                                            Remove
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <div
                        className="card"
                        style={{ marginTop: '24px' }}
                    >
                        <h3>
                            Total: {totalPrice} RSD
                        </h3>

                        <div
                            style={{
                                display: 'flex',
                                gap: '12px',
                                marginTop: '16px'
                            }}
                        >
                            <button
                                className="btn-primary"
                                onClick={handleConfirm}
                                disabled={saving}
                            >
                                {saving
                                    ? 'Creating...'
                                    : 'Confirm Reservation'}
                            </button>

                            <button
                                className="btn-ghost"
                                onClick={() =>
                                    navigate('/reservations/create')
                                }
                            >
                                Continue Selecting
                            </button>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}

export default ConfirmReservation;