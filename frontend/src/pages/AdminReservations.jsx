import { useEffect, useState } from 'react';
import { getReservations } from '../services/api';
import Modal from '../components/Modal';

const STATUS_OPTIONS = ['', 'UPCOMING', 'ACTIVE', 'COMPLETED', 'CANCELLED'];

function AdminReservations() {
  const [reservations, setReservations] = useState([]);
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [selected, setSelected] = useState(null);

  const fetchReservations = async (s) => {
    setLoading(true);
    setError('');
    try {
      const params = s ? { status: s } : {};
      const res = await getReservations(params);
      setReservations(res.data);
    } catch {
      setError('Failed to load reservations.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchReservations(status); }, [status]);

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      <div className="page-header">
        <div>
          <div className="page-title">All Reservations</div>
          <div className="page-subtitle">Admin view</div>
        </div>

        {/* Filter */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <span style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)' }}>Status</span>
          <select
            className="form-select"
            value={status}
            onChange={e => { setStatus(e.target.value); setSelected(null); }}
            style={{ minWidth: '140px' }}
          >
            {STATUS_OPTIONS.map(s => (
              <option key={s} value={s}>{s || 'All'}</option>
            ))}
          </select>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}

      <div style={{
        display: 'grid',
        gridTemplateColumns: selected ? '1fr 420px' : '1fr',
        gap: '32px',
        alignItems: 'start',
      }}>

        {/* ─── Tabela ─── */}
        <div>
          {loading ? (
            <div>Loading...</div>
          ) : reservations.length === 0 ? (
            <div className="card">No reservations found.</div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>User</th>
                  <th>Date(s)</th>
                  <th>Status</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {reservations.map(r => (
                  <tr
                    key={r.reservationId}
                    onClick={() => setSelected(selected?.reservationId === r.reservationId ? null : r)}
                    style={{
                      cursor: 'pointer',
                      background: selected?.reservationId === r.reservationId
                        ? 'rgba(139,90,60,0.06)' : undefined,
                    }}
                  >
                    <td>#{r.reservationId}</td>
                    <td style={{ fontSize: '12px', color: 'var(--bronze)' }}>{r.applicationUserId.substring(0, 8)}…</td>
                    <td>{r.items?.map(i => i.date).join(', ')}</td>
                    <td>
                      <span className={`badge badge-${r.status.toLowerCase()}`}>{r.status}</span>
                    </td>
                    <td>{r.totalPrice} RSD</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* ─── Detail panel ─── */}
        {selected && (
          <div className="card" style={{ padding: '28px', position: 'sticky', top: '100px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <div style={{
                fontFamily: "'Cormorant Garamond', serif",
                fontSize: '22px', fontStyle: 'italic', color: 'var(--mahogany)',
              }}>
                Reservation #{selected.reservationId}
              </div>
              <button
                style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: 'var(--bronze)' }}
                onClick={() => setSelected(null)}
              >×</button>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '20px' }}>
              <div>
                <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '4px' }}>Status</div>
                <span className={`badge badge-${selected.status.toLowerCase()}`}>{selected.status}</span>
              </div>
              <div>
                <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '4px' }}>Total Price</div>
                <div style={{ fontWeight: 500 }}>{selected.totalPrice} RSD</div>
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '4px' }}>User ID</div>
                <div style={{ fontSize: '12px', wordBreak: 'break-all' }}>{selected.applicationUserId}</div>
              </div>
            </div>

            <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '10px' }}>Items</div>
            <table>
              <thead>
                <tr><th>#</th><th>Date</th><th>Court</th><th>Time</th><th>Price</th></tr>
              </thead>
              <tbody>
                {selected.items.map(i => (
                  <tr key={i.rowNumber}>
                    <td>{i.rowNumber}</td>
                    <td>{i.date}</td>
                    <td>{i.courtName || '—'}</td>
                    <td>{i.startTime} – {i.endTime}</td>
                    <td>{i.price} RSD</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminReservations;
