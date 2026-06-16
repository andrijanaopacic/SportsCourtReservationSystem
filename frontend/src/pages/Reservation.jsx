import { useEffect, useState } from 'react';
import {
  getMyReservations,
  getReservationById,
  cancelReservation,
  updateReservation,
  getCourts,
  getTimeSlotsByCourt,
  getCourtReservations,
} from '../services/api';
import Modal from '../components/Modal';

const STATUSES = ['UPCOMING', 'ACTIVE', 'COMPLETED', 'CANCELLED'];

/* ── helper ── */
const fmt = (t) => (typeof t === 'string' ? t.substring(0, 5) : t);

function AddItemWizard({ onAdd, onCancel }) {
  const [courts, setCourts] = useState([]);
  const [selectedCourt, setSelectedCourt] = useState(null);
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split('T')[0]);
  const [slots, setSlots] = useState([]);
  const [takenSlotIds, setTakenSlotIds] = useState(new Set());
  const [loadingSlots, setLoadingSlots] = useState(false);

  // Učitaj terene
  useEffect(() => {
    getCourts().then(r => setCourts(r.data)).catch(() => {});
  }, []);

  // Kad se odabere teren + datum, učitaj termine i rezervacije
  useEffect(() => {
    if (!selectedCourt) return;
    setLoadingSlots(true);
    Promise.all([
      getTimeSlotsByCourt(selectedCourt.courtId),
      getCourtReservations(selectedCourt.courtId, { startDate: selectedDate, endDate: selectedDate }),
    ])
      .then(([slotsRes, resRes]) => {
        setSlots(slotsRes.data);
        const taken = new Set(
          resRes.data
            .flatMap(r => r.items || [])
            .filter(i => String(i.date).substring(0, 10) === selectedDate)
            .map(i => i.timeSlotId)
        );
        setTakenSlotIds(taken);
      })
      .catch(() => {})
      .finally(() => setLoadingSlots(false));
  }, [selectedCourt, selectedDate]);

  const handleSlotClick = (slot) => {
    if (takenSlotIds.has(slot.timeSlotId)) return;
    onAdd({ timeSlotId: slot.timeSlotId, date: selectedDate });
  };

  return (
    <div style={{ marginBottom: '20px', padding: '16px', border: '1px solid var(--sage)', borderRadius: '8px' }}>
      <div style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '12px' }}>
        Add Item
      </div>

      {/* Izbor terena */}
      <div className="form-group" style={{ marginBottom: '12px' }}>
        <label className="form-label">Court</label>
        <select
          className="form-select"
          value={selectedCourt?.courtId ?? ''}
          onChange={e => {
            const c = courts.find(c => c.courtId === Number(e.target.value));
            setSelectedCourt(c || null);
            setSlots([]);
          }}
        >
          <option value="">— Select court —</option>
          {courts.map(c => (
            <option key={c.courtId} value={c.courtId}>
              {c.name} — {c.location}
            </option>
          ))}
        </select>
      </div>

      {/* Izbor datuma */}
      {selectedCourt && (
        <div className="form-group" style={{ marginBottom: '16px' }}>
          <label className="form-label">Date</label>
          <input
            type="date"
            className="form-input"
            value={selectedDate}
            min={new Date().toISOString().split('T')[0]}
            onChange={e => setSelectedDate(e.target.value)}
            style={{ maxWidth: '200px' }}
          />
        </div>
      )}

      {/* Termini */}
      {selectedCourt && (
        <div>
          <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '10px' }}>
            Available Time Slots — {selectedCourt.name}
          </div>
          {loadingSlots ? (
            <div style={{ fontSize: '13px', color: 'var(--bronze)' }}>Loading slots...</div>
          ) : slots.length === 0 ? (
            <div style={{ fontSize: '13px', color: 'var(--bronze)' }}>No time slots for this court.</div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '8px' }}>
              {slots.map(slot => {
                const taken = takenSlotIds.has(slot.timeSlotId);
                return (
                  <div
                    key={slot.timeSlotId}
                    onClick={() => handleSlotClick(slot)}
                    style={{
                      padding: '12px',
                      borderRadius: '8px',
                      cursor: taken ? 'not-allowed' : 'pointer',
                      background: taken ? '#c62828' : '#2e7d32',
                      color: '#fff',
                      textAlign: 'center',
                      opacity: taken ? 0.7 : 1,
                      userSelect: 'none',
                      transition: 'opacity 0.15s',
                    }}
                  >
                    <div style={{ fontWeight: 600, fontSize: '13px' }}>
                      {fmt(slot.startTime)} – {fmt(slot.endTime)}
                    </div>
                    <div style={{ fontSize: '11px', marginTop: '4px', opacity: 0.85 }}>
                      {taken ? 'Reserved' : `${slot.price} RSD`}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      <button
        className="btn-danger"
        onClick={onCancel}
        style={{ marginTop: '14px', fontSize: '11px' }}
      >
        Cancel
      </button>
    </div>
  );
}

/* ══════════════════════════════════════════════════════════ */

function ReservationsList() {
  const [reservations, setReservations] = useState([]);
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const [selectedReservation, setSelectedReservation] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [editing, setEditing] = useState(false);
  const [editStatus, setEditStatus] = useState('');
  const [editItems, setEditItems] = useState([]);
  const [saving, setSaving] = useState(false);
  const [showWizard, setShowWizard] = useState(false);

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

  useEffect(() => { fetchMyReservations(); }, []);

  const handleCancel = async (id) => {
    try {
      await cancelReservation(id);
      await fetchMyReservations();
      if (selectedReservation?.reservationId === id) {
        setSelectedReservation(prev => ({ ...prev, status: 'CANCELLED' }));
        setEditing(false);
      }
      setModalMessage('Reservation cancelled.');
    } catch (err) {
      setModalMessage(err.response?.data || 'Cannot cancel reservation.');
    }
  };

  const handleViewDetail = async (id) => {
    setDetailLoading(true);
    setEditing(false);
    setShowWizard(false);
    try {
      const res = await getReservationById(id);
      setSelectedReservation(res.data);
      setEditStatus(res.data.status);
      setEditItems(res.data.items.map(i => ({ timeSlotId: i.timeSlotId, date: i.date })));
    } catch {
      setModalMessage('Failed to load reservation details.');
    } finally {
      setDetailLoading(false);
    }
  };

  const handleUpdate = async () => {
    setSaving(true);
    try {
      const res = await updateReservation(selectedReservation.reservationId, {
        status: editStatus,
        items: editItems,
      });
      setSelectedReservation(res.data);
      setEditing(false);
      setShowWizard(false);
      await fetchMyReservations();
      setModalMessage('Reservation updated successfully.');
    } catch (err) {
      setModalMessage(err.response?.data || 'Failed to update reservation.');
    } finally {
      setSaving(false);
    }
  };

  const removeItem = (idx) => {
    setEditItems(prev => prev.filter((_, i) => i !== idx));
  };

  const handleAddItem = ({ timeSlotId, date }) => {
    setEditItems(prev => [...prev, { timeSlotId, date }]);
    setShowWizard(false);
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

      <div style={{
        display: 'grid',
        gridTemplateColumns: selectedReservation ? '1fr 420px' : '1fr',
        gap: '32px',
        alignItems: 'start',
      }}>

        {/* ─── Tabela ─── */}
        <div>
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
                  <th>Status</th>
                  <th>Total</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {reservations.map((res) => (
                  <tr
                    key={res.reservationId}
                    style={{
                      cursor: 'pointer',
                      background: selectedReservation?.reservationId === res.reservationId
                        ? 'rgba(139,90,60,0.06)' : undefined,
                    }}
                  >
                    <td onClick={() => handleViewDetail(res.reservationId)}>#{res.reservationId}</td>
                    <td onClick={() => handleViewDetail(res.reservationId)}>
                      {res.items?.map(i => i.date).join(', ')}
                    </td>
                    <td onClick={() => handleViewDetail(res.reservationId)}>
                      <span className={`badge badge-${res.status.toLowerCase()}`}>{res.status}</span>
                    </td>
                    <td onClick={() => handleViewDetail(res.reservationId)}>{res.totalPrice} RSD</td>
                    <td className="actions">
                      {res.status !== 'CANCELLED' && (
                        <button className="btn-danger" onClick={() => handleCancel(res.reservationId)}>
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

        {/* ─── Detail/Edit panel ─── */}
        {selectedReservation && (
          <div className="card" style={{ padding: '28px', position: 'sticky', top: '100px' }}>
            {detailLoading ? (
              <div>Loading details...</div>
            ) : (
              <>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                  <div style={{
                    fontFamily: "'Cormorant Garamond', serif",
                    fontSize: '22px', fontStyle: 'italic', color: 'var(--mahogany)',
                  }}>
                    Reservation #{selectedReservation.reservationId}
                  </div>
                  <button
                    style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: 'var(--bronze)' }}
                    onClick={() => { setSelectedReservation(null); setEditing(false); setShowWizard(false); }}
                  >
                    ×
                  </button>
                </div>

                {!editing ? (
                  /* ── VIEW MODE ── */
                  <>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '20px' }}>
                      <div>
                        <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '4px' }}>Status</div>
                        <span className={`badge badge-${selectedReservation.status.toLowerCase()}`}>{selectedReservation.status}</span>
                      </div>
                      <div>
                        <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '4px' }}>Total Price</div>
                        <div style={{ fontWeight: 500 }}>{selectedReservation.totalPrice} RSD</div>
                      </div>
                    </div>

                    <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '10px' }}>Items</div>
                    <table style={{ marginBottom: '20px' }}>
                      <thead>
                        <tr><th>#</th><th>Date</th><th>Time Slot</th><th>Price</th></tr>
                      </thead>
                      <tbody>
                        {selectedReservation.items.map(i => (
                          <tr key={i.rowNumber}>
                            <td>{i.rowNumber}</td>
                            <td>{i.date}</td>
                            <td>Slot #{i.timeSlotId}</td>
                            <td>{i.price} RSD</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>

                    {selectedReservation.status !== 'CANCELLED' && (
                      <div className="actions">
                        <button className="btn" onClick={() => {
                          setEditStatus(selectedReservation.status);
                          setEditItems(selectedReservation.items.map(i => ({ timeSlotId: i.timeSlotId, date: i.date })));
                          setEditing(true);
                          setShowWizard(false);
                        }}>Edit</button>
                        <button className="btn-danger" onClick={() => handleCancel(selectedReservation.reservationId)}>Cancel</button>
                      </div>
                    )}
                  </>
                ) : (
                  /* ── EDIT MODE ── */
                  <>
                    <div className="form-group" style={{ marginBottom: '16px' }}>
                      <label className="form-label">Status</label>
                      <select className="form-select" value={editStatus} onChange={e => setEditStatus(e.target.value)}>
                        {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
                      </select>
                    </div>

                    <div style={{ fontSize: '10px', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--bronze)', marginBottom: '10px' }}>Items</div>

                    {editItems.length > 0 && (
                      <table style={{ marginBottom: '12px' }}>
                        <thead>
                          <tr><th>Date</th><th>Slot ID</th><th></th></tr>
                        </thead>
                        <tbody>
                          {editItems.map((item, idx) => (
                            <tr key={idx}>
                              <td>{item.date}</td>
                              <td>#{item.timeSlotId}</td>
                              <td>
                                <button className="btn-danger" onClick={() => removeItem(idx)} style={{ padding: '4px 8px' }}>✕</button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    )}

                    {/* Wizard za dodavanje stavke */}
                    {showWizard ? (
                      <AddItemWizard
                        onAdd={handleAddItem}
                        onCancel={() => setShowWizard(false)}
                      />
                    ) : (
                      <button
                        className="btn-ghost"
                        onClick={() => setShowWizard(true)}
                        style={{ marginBottom: '20px', fontSize: '11px' }}
                      >
                        + Add Item
                      </button>
                    )}

                    <div className="actions">
                      <button className="btn-primary" onClick={handleUpdate} disabled={saving}>
                        {saving ? 'Saving...' : 'Save'}
                      </button>
                      <button className="btn-danger" onClick={() => { setEditing(false); setShowWizard(false); }}>Discard</button>
                    </div>
                  </>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default ReservationsList;
