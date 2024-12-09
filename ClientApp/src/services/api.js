import axios from '../axiosConfig';

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
  getFeatured: () => api.get('/items/featured'),
  search: (query) => api.get(`/items/search?query=${query}`)
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

export const adminApi = {
  // Users
  getAllUsers: () => api.get('/admin/users'),
  updateUser: (userId, userData) => api.put(`/admin/users/${userId}`, userData),
  deleteUser: (userId) => api.delete(`/admin/users/${userId}`),
  
  // Products
  createProduct: (productData) => api.post('/admin/products', productData),
  updateProduct: (productId, productData) => api.put(`/admin/products/${productId}`, productData),
  deleteProduct: (productId) => api.delete(`/admin/products/${productId}`),
  
  // Categories
  createCategory: (categoryData) => api.post('/admin/categories', categoryData),
  updateCategory: (categoryId, categoryData) => api.put(`/admin/categories/${categoryId}`, categoryData),
  deleteCategory: (categoryId) => api.delete(`/admin/categories/${categoryId}`),
  
  // Orders
  updateOrderStatus: (orderId, status) => api.put(`/admin/orders/${orderId}/status`, { status })
};

export default api;
