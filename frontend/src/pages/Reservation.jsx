import { useEffect, useState } from 'react';
import { getMyReservations, cancelReservation } from '../services/api';
import Modal from '../components/Modal';

function ReservationsList() {
  const [reservations, setReservations] = useState([]);
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const fetchMyReservations = async () => {
    setLoading(true);
    setError('');

    try {
      const res = await getMyReservations();
      setReservations(res.data);
    } catch {
      setError('Failed to load your reservations.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMyReservations();
  }, []);

  const handleCancel = async (id) => {
    try {
      await cancelReservation(id);

      // 🔥 refresh after cancel
      await fetchMyReservations();

      setModalMessage('Reservation cancelled.');
    } catch (err) {
      setModalMessage(
        err.response?.data || 'Cannot cancel reservation.'
      );
    }
  };

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      <div className="page-header">
        <div>
          <div className="page-title">My Reservations</div>
          <div className="page-subtitle">Only your bookings</div>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {loading ? (
        <div>Loading...</div>
      ) : reservations.length === 0 ? (
        <div className="card">You have no reservations.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Date(s)</th>
              <th>Time Slot(s)</th>
              <th>Status</th>
              <th>Total Price</th>
              <th></th>
            </tr>
          </thead>

          <tbody>
            {reservations.map((res) => (
              <tr key={res.reservationId}>
                <td>#{res.reservationId}</td>

                <td>
                  {res.items?.map(i => i.date).join(', ')}
                </td>

                <td>
                  {res.items?.map(i => `Slot #${i.timeSlotId}`).join(', ')}
                </td>

                <td>
                  <span className={`badge badge-${res.status.toLowerCase()}`}>
                    {res.status}
                  </span>
                </td>

                <td>{res.totalPrice} RSD</td>

                <td className="actions">
                  {res.status !== 'CANCELLED' && (
                    <button
                      className="btn-danger"
                      onClick={() => handleCancel(res.reservationId)}
                    >
                      Cancel
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default ReservationsList;