import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Add request interceptor to include auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// API endpoints
export const productsApi = {
  getAll: (categoryId) => api.get(`/items${categoryId ? `?categoryId=${categoryId}` : ''}`),
  getById: (id) => api.get(`/items/${id}`),
  getFeatured: () => api.get('/home/featured-products'),
  search: (query) => api.get(`/home/search?query=${query}`)
};

export const cartApi = {
  addItem: (itemId, sizeId, quantity) => api.post('/items/addtocart', { itemId, sizeId, quantity }),
  getCart: () => api.get('/cart'),
  updateItem: (itemId, quantity) => api.put(`/cart/items/${itemId}`, { quantity }),
  removeItem: (itemId) => api.delete(`/cart/items/${itemId}`),
  checkout: (paymentInfo) => api.post('/cart/checkout', paymentInfo)
};

export const authApi = {
  login: (credentials) => api.post('/account/login', credentials),
  register: (userData) => api.post('/account/register', userData),
  logout: () => api.post('/account/logout'),
  getProfile: () => api.get('/account/profile'),
  updateProfile: (profileData) => api.put('/account/profile', profileData),
  changePassword: (passwordData) => api.post('/account/change-password', passwordData)
};

export const ordersApi = {
  getAll: () => api.get('/account/orders'),
  getById: (id) => api.get(`/account/orders/${id}`)
};

export const categoriesApi = {
  getAll: () => api.get('/home/categories')
};

export default api;
