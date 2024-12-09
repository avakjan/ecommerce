// src/index.js
import React from 'react';
import ReactDOM from 'react-dom/client'; // Updated import for React 18
import App from './App';
import { AuthProvider } from './context/AuthContext';
import { BrowserRouter } from 'react-router-dom';

const container = document.getElementById('root');
const root = ReactDOM.createRoot(container);

root.render(
  <React.StrictMode>
    <BrowserRouter> {/* Single Router Instance */}
      <AuthProvider> {/* Provides Auth Context */}
        <App /> {/* Main App Component */}
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
