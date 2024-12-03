import React, { createContext, useState, useContext, useEffect } from 'react';
import { cartApi } from '../services/api';

const CartContext = createContext(null);

export const CartProvider = ({ children }) => {
  const [cart, setCart] = useState({ items: [], total: 0 });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchCart();
  }, []);

  const fetchCart = async () => {
    try {
      const response = await cartApi.getCart();
      setCart(response.data);
    } catch (error) {
      console.error('Failed to fetch cart:', error);
    } finally {
      setLoading(false);
    }
  };

  const addToCart = async (itemId, sizeId, quantity) => {
    try {
      await cartApi.addItem(itemId, sizeId, quantity);
      await fetchCart();
    } catch (error) {
      console.error('Failed to add item to cart:', error);
      throw error;
    }
  };

  const updateQuantity = async (itemId, quantity) => {
    try {
      await cartApi.updateItem(itemId, quantity);
      await fetchCart();
    } catch (error) {
      console.error('Failed to update cart item:', error);
      throw error;
    }
  };

  const removeItem = async (itemId) => {
    try {
      await cartApi.removeItem(itemId);
      await fetchCart();
    } catch (error) {
      console.error('Failed to remove item from cart:', error);
      throw error;
    }
  };

  const checkout = async (paymentInfo) => {
    try {
      const response = await cartApi.checkout(paymentInfo);
      await fetchCart(); // Refresh cart after checkout
      return response.data;
    } catch (error) {
      console.error('Checkout failed:', error);
      throw error;
    }
  };

  const value = {
    cart,
    loading,
    addToCart,
    updateQuantity,
    removeItem,
    checkout,
    itemCount: cart.items.reduce((total, item) => total + item.quantity, 0)
  };

  return (
    <CartContext.Provider value={value}>
      {!loading && children}
    </CartContext.Provider>
  );
};

export const useCart = () => {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
};
