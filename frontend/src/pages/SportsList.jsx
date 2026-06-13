import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getSports, getSportById, deleteSport } from '../services/api';
import Modal from '../components/Modal';

function SportsList() {
  const [sports, setSports] = useState([]);
  const [searchName, setSearchName] = useState('');
  const [error, setError] = useState('');
  const [modalMessage, setModalMessage] = useState('');
  const [detailsSport, setDetailsSport] = useState(null);

  useEffect(() => { fetchSports(); }, []);

  const fetchSports = async () => {
    try {
      const res = await getSports({});
      setSports(res.data);
    } catch {
      setError('Failed to load sports.');
    }
  };

  const handleSearch = async () => {
    try {
      const res = await getSports({ name: searchName });
      setSports(res.data);
    } catch {
      setError('Failed to load sports.');
    }
  };

  const handleShowAll = async () => {
    setSearchName('');
    try {
      const res = await getSports({});
      setSports(res.data);
    } catch {
      setError('Failed to load sports.');
    }
  };

  const handleDetails = async (id) => {
    try {
      const res = await getSportById(id);
      setDetailsSport(res.data);
    } catch {
      setModalMessage('Failed to load sport details.');
    }
  };

  const handleDelete = async (id) => {
    try {
      await deleteSport(id);
      fetchSports();
    } catch (err) {
      setModalMessage(err.response?.data || 'Cannot delete sport.');
    }
  };

  return (
    <div className="page">
      <Modal message={modalMessage} onClose={() => setModalMessage('')} />

      {/* Details Modal */}
      {detailsSport && (
        <div style={{
          position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
          background: 'rgba(78, 34, 15, 0.4)', display: 'flex',
          alignItems: 'center', justifyContent: 'center', zIndex: 1000
        }}>
          <div style={{
            background: 'var(--cream)', padding: '48px',
            maxWidth: '520px', width: '90%',
            borderTop: '2px solid var(--bronze)'
          }}>
            <div style={{
              fontFamily: 'Cormorant Garamond, serif',
              fontSize: '32px', fontWeight: 300, fontStyle: 'italic',
              color: 'var(--mahogany)', marginBottom: '8px'
            }}>
              {detailsSport.name}
            </div>
            <div style={{
              fontSize: '11px', letterSpacing: '0.12em',
              textTransform: 'uppercase', color: 'var(--bronze)',
              marginBottom: '32px'
            }}>
              Max {detailsSport.maxPlayers} players
            </div>

            <div style={{
              fontSize: '10px', letterSpacing: '0.15em',
              textTransform: 'uppercase', color: 'var(--bronze)',
              marginBottom: '12px'
            }}>
              Courts
            </div>

            {detailsSport.courts && detailsSport.courts.length > 0 ? (
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--mahogany)' }}>
                    <th style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)', padding: '0 0 8px', textAlign: 'left' }}>Name</th>
                    <th style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)', padding: '0 0 8px', textAlign: 'left' }}>Location</th>
                    <th style={{ fontSize: '10px', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--bronze)', padding: '0 0 8px', textAlign: 'left' }}>Price / hr</th>
                  </tr>
                </thead>
                <tbody>
                  {detailsSport.courts.map(court => (
                    <tr key={court.courtId} style={{ borderBottom: '1px solid var(--cream-dark)' }}>
                      <td style={{ padding: '12px 0', fontFamily: 'Cormorant Garamond, serif', fontSize: '16px' }}>{court.name}</td>
                      <td style={{ padding: '12px 0', fontSize: '13px', fontWeight: 300 }}>{court.location}</td>
                      <td style={{ padding: '12px 0', fontSize: '13px', fontWeight: 300 }}>{court.pricePerHour} RSD</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p style={{ fontSize: '13px', color: 'var(--sage)', fontWeight: 300 }}>
                No courts assigned to this sport yet.
              </p>
            )}

            <div style={{ marginTop: '32px' }}>
              <button className="btn-primary" onClick={() => setDetailsSport(null)}>Close</button>
            </div>
          </div>
        </div>
      )}

      <div className="page-header">
        <div>
          <div className="page-title">Sports</div>
          <div className="page-subtitle">All available disciplines</div>
        </div>
        <Link to="/sports/new">
          <button className="btn-primary">+ Add Sport</button>
        </Link>
      </div>

      <div className="filter-bar">
        <div className="form-group">
          <label className="form-label">Search</label>
          <input
            className="form-input"
            placeholder="Sport name..."
            value={searchName}
            onChange={e => setSearchName(e.target.value)}
            style={{ width: '220px' }}
          />
        </div>
        <button className="btn-ghost" onClick={handleSearch}>Search</button>
        <button className="btn-ghost" onClick={handleShowAll}>Show All</button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      <table>
        <thead>
          <tr>
            <th>Sport</th>
            <th>Max Players</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {sports.map(sport => (
            <tr key={sport.sportId}>
              <td className="td-name">{sport.name}</td>
              <td>{sport.maxPlayers}</td>
              <td className="actions">
                <button className="btn-ghost" onClick={() => handleDetails(sport.sportId)}>
                  Details
                </button>
                <span className="divider">|</span>
                <Link to={`/sports/${sport.sportId}`}>
                  <button className="btn-ghost">Edit</button>
                </Link>
                <span className="divider">|</span>
                <button className="btn-danger" onClick={() => handleDelete(sport.sportId)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default SportsList;