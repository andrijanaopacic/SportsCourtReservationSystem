import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getSports, deleteSport } from '../services/api';

function SportsList() {
  const [sports, setSports] = useState([]);
  const [searchName, setSearchName] = useState('');
  const [error, setError] = useState('');

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

  const handleDelete = async (id) => {
    try {
      await deleteSport(id);
      fetchSports();
    } catch (err) {
      alert(err.response?.data || 'Cannot delete sport.');
    }
  };

  return (
    <div className="page">
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