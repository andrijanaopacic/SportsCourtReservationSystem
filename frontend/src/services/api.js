import axios from 'axios';

const api = axios.create({
    baseURL: 'https://localhost:7070/api'
});

// Automatically attach JWT token to every request if present in localStorage
api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Sports
export const getSports = (params) => api.get('/sports', { params });
export const getSportById = (id) => api.get(`/sports/${id}`);
export const createSport = (data) => api.post('/sports', data);
export const updateSport = (id, data) => api.put(`/sports/${id}`, data);
export const deleteSport = (id) => api.delete(`/sports/${id}`);

// Courts
export const getCourts = (params) => api.get('/courts', { params });
export const getCourtById = (id) => api.get(`/courts/${id}`);
export const createCourt = (data) => api.post('/courts', data);
export const updateCourt = (id, data) => api.put(`/courts/${id}`, data);
export const deleteCourt = (id) => api.delete(`/courts/${id}`);

// TimeSlots — date param je opcionalan; ako se prosledi, bek filtrira isAvailable po datumu
export const getTimeSlots = (params) => api.get('/timeslots', { params });
export const getTimeSlotById = (id) => api.get(`/timeslots/${id}`);
export const getTimeSlotsByCourt = (courtId, date) =>
    api.get(`/timeslots/by-court/${courtId}`, {
        params: date ? { date } : {}
    });
export const createTimeSlot = (data) => api.post('/timeslots', data);
export const updateTimeSlot = (id, data) => api.put(`/timeslots/${id}`, data);
export const deleteTimeSlot = (id) => api.delete(`/timeslots/${id}`);

// Auth
export const login = (data) => api.post('/auth/login', data);
export const register = (data) => api.post('/auth/register', data);
export const getMe = () => api.get('/auth/me');

// Reservations
export const getReservations = (params) => api.get('/reservations', { params });
export const getMyReservations = () => api.get('/reservations/my');
export const getReservationById = (id) => api.get(`/reservations/${id}`);
export const createReservation = (data) => api.post('/reservations', data);
export const updateReservation = (id, data) => api.put(`/reservations/${id}`, data);
export const cancelReservation = (id) => api.put(`/reservations/${id}/cancel`);
export const getCourtReservations = (courtId, date) =>
    api.get(`/reservations/court/${courtId}`, {
        params: { date }
    });
export const getCourtCalendar = (courtId, year, month) =>
    api.get(`/reservations/court/${courtId}/calendar`, {
        params: { year, month }
    });
export const getTimeSlotsByCourtReservations = (courtId, dateStr) => {
    return api.get(`/reservations/court/${courtId}/slots`, {
        params: { date: dateStr }
    });
};
