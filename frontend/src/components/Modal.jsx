function Modal({ message, onClose }) {
  if (!message) return null;

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
      background: 'rgba(78, 34, 15, 0.4)', display: 'flex',
      alignItems: 'center', justifyContent: 'center', zIndex: 1000
    }}>
      <div style={{
        background: 'var(--cream)', padding: '48px',
        maxWidth: '420px', width: '90%',
        borderTop: '2px solid var(--bronze)'
      }}>
        <p style={{
          fontFamily: 'Cormorant Garamond, serif',
          fontSize: '20px', fontWeight: 300,
          color: 'var(--mahogany)', marginBottom: '32px',
          lineHeight: 1.5
        }}>
          {message}
        </p>
        <button className="btn-primary" onClick={onClose}>Close</button>
      </div>
    </div>
  );
}

export default Modal;