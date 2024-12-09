import React, { useState } from 'react';
import {
  Container,
  Grid,
  Paper,
  Box,
  Tabs,
  Tab,
  Typography,
} from '@mui/material';
import ProductsManager from './ProductsManager';
import CategoriesManager from './CategoriesManager';
import OrdersManager from './OrdersManager';
import UsersManager from './UsersManager';

function TabPanel({ children, value, index, ...other }) {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`admin-tabpanel-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const Dashboard = () => {
  const [tabValue, setTabValue] = useState(0);

  const handleTabChange = (event, newValue) => {
    setTabValue(newValue);
  };

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Admin Dashboard
      </Typography>

      <Paper elevation={3}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs 
            value={tabValue} 
            onChange={handleTabChange} 
            aria-label="admin dashboard tabs"
          >
            <Tab label="Products" />
            <Tab label="Categories" />
            <Tab label="Orders" />
            <Tab label="Users" />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          <ProductsManager />
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <CategoriesManager />
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <OrdersManager />
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <UsersManager />
        </TabPanel>
      </Paper>
    </Container>
  );
};

export default Dashboard;