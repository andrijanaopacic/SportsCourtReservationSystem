import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { createReservation } from '../services/api';

function ConfirmReservation() {
    const navigate = useNavigate();
    const location = useLocation();

    // Stavke dolaze kroz router state — bez localStorage
    const [items, setItems] = useState(location.state?.items || []);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [confirmedTotal, setConfirmedTotal] = useState(null);

    const removeItem = (index) => {
        setItems(prev => prev.filter((_, i) => i !== index));
    };

    const handleConfirm = async () => {
        if (items.length === 0) return;

        try {
            setSaving(true);
            setError('');

            const result = await createReservation({
                items: items.map(item => ({
                    timeSlotId: item.timeSlotId
                }))
            });

            // Ukupan total preuzimamo iz bek response-a
            setConfirmedTotal(result.data?.totalPrice ?? null);

            navigate('/reservations');
        } catch (err) {
            console.error(err);

            const responseData = err.response?.data;

            if (Array.isArray(responseData)) {
                // FluentValidation niz grešaka
                const validationMessages = responseData
                    .map(e => `${e.propertyName}: ${e.errorMessage}`)
                    .join(', ');
                setError(validationMessages);
            } else if (typeof responseData === 'string') {
                // Custom string poruka sa beka (npr. "Termin X je već rezervisan.")
                setError(responseData);
            } else {
                setError(responseData?.message || 'Uspostavljanje rezervacije nije uspelo.');
            }
        } finally {
            setSaving(false);
        }
    };

    // Lokalni total samo za UX prikaz pre slanja — konačan total dolazi sa beka
    const previewTotal = items.reduce(
        (sum, item) => sum + (item.price || 0),
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
                    No reservation items selected.{' '}
                    <button className="btn-ghost" onClick={() => navigate('/courts')}>
                        Browse courts
                    </button>
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
                                    <td>{item.price} RSD</td>
                                    <td>
                                        <button
                                            className="btn-danger"
                                            onClick={() => removeItem(index)}
                                        >
                                            Remove
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <div className="card" style={{ marginTop: 24 }}>
                        {/* Prikazujemo lokalni preview total dok se čeka potvrda sa beka */}
                        <h3>Total: {confirmedTotal !== null ? confirmedTotal : previewTotal} RSD</h3>

                        <div style={{ display: 'flex', gap: 12, marginTop: 16 }}>
                            <button
                                className="btn-primary"
                                onClick={handleConfirm}
                                disabled={saving}
                            >
                                {saving ? 'Creating...' : 'Confirm Reservation'}
                            </button>

                            <button
                                className="btn-ghost"
                                onClick={() => navigate(-1)}
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
