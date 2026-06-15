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
export const getCourtsBySport = (sportId) => api.get(`/courts/by-sport/${sportId}`);

// TimeSlots 
export const getTimeSlots = (params) => api.get('/timeslots', { params });
export const getTimeSlotById = (id) => api.get(`/timeslots/${id}`);
export const getTimeSlotsByCourt = (courtId) => api.get(`/timeslots/by-court/${courtId}`);
export const createTimeSlot = (data) => api.post('/timeslots', data);
export const updateTimeSlot = (id, data) => api.put(`/timeslots/${id}`, data);
export const deleteTimeSlot = (id) => api.delete(`/timeslots/${id}`);

// Auth
export const login = (data) => api.post('/auth/login', data);
export const register = (data) => api.post('/auth/register', data);
export const getMe = () => api.get('/auth/me');
