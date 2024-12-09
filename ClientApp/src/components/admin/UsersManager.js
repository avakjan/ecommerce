import React, { useState, useEffect } from 'react';
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Alert,
  CircularProgress,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
} from '@mui/material';
import {
  Visibility as ViewIcon,
  Block as BlockIcon,
  CheckCircle as UnblockIcon,
} from '@mui/icons-material';
import { adminApi } from '../../services/api';

const UserRoles = {
  USER: 'User',
  ADMIN: 'Admin'
};

const roleColors = {
  [UserRoles.USER]: 'primary',
  [UserRoles.ADMIN]: 'secondary'
};

const UsersManager = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [openDialog, setOpenDialog] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const response = await adminApi.getAllUsers();
      setUsers(response.data);
    } catch (err) {
      setError('Failed to fetch users');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenDialog = (user) => {
    setSelectedUser(user);
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setSelectedUser(null);
  };

  const handleRoleChange = async (userId, newRole) => {
    setLoading(true);
    setError('');
    setSuccess('');

    try {
      await adminApi.updateUserRole(userId, newRole);
      setSuccess('User role updated successfully');
      fetchUsers();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update user role');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleBlock = async (userId, isBlocked) => {
    setLoading(true);
    setError('');
    setSuccess('');

    try {
      if (isBlocked) {
        await adminApi.unblockUser(userId);
        setSuccess('User unblocked successfully');
      } else {
        await adminApi.blockUser(userId);
        setSuccess('User blocked successfully');
      }
      fetchUsers();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update user status');
    } finally {
      setLoading(false);
    }
  };

  if (loading && users.length === 0) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((user) => (
              <TableRow key={user.id}>
                <TableCell>{`${user.firstName} ${user.lastName}`}</TableCell>
                <TableCell>{user.email}</TableCell>
                <TableCell>
                  <Chip
                    label={user.role}
                    color={roleColors[user.role]}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  <Chip
                    label={user.isBlocked ? 'Blocked' : 'Active'}
                    color={user.isBlocked ? 'error' : 'success'}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  <IconButton
                    color="primary"
                    onClick={() => handleOpenDialog(user)}
                  >
                    <ViewIcon />
                  </IconButton>
                  <IconButton
                    color={user.isBlocked ? 'success' : 'error'}
                    onClick={() => handleToggleBlock(user.id, user.isBlocked)}
                  >
                    {user.isBlocked ? <UnblockIcon /> : <BlockIcon />}
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* User Details Dialog */}
      <Dialog
        open={openDialog}
        onClose={handleCloseDialog}
        maxWidth="sm"
        fullWidth
      >
        {selectedUser && (
          <>
            <DialogTitle>
              User Details
            </DialogTitle>
            <DialogContent>
              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={12}>
                  <Typography variant="h6">Personal Information</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="subtitle2">First Name</Typography>
                  <Typography>{selectedUser.firstName}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="subtitle2">Last Name</Typography>
                  <Typography>{selectedUser.lastName}</Typography>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="subtitle2">Email</Typography>
                  <Typography>{selectedUser.email}</Typography>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="subtitle2">Registration Date</Typography>
                  <Typography>
                    {new Date(selectedUser.registrationDate).toLocaleDateString()}
                  </Typography>
                </Grid>

                <Grid item xs={12} sx={{ mt: 2 }}>
                  <FormControl fullWidth>
                    <InputLabel>Role</InputLabel>
                    <Select
                      value={selectedUser.role}
                      onChange={(e) => handleRoleChange(selectedUser.id, e.target.value)}
                      label="Role"
                    >
                      {Object.values(UserRoles).map((role) => (
                        <MenuItem key={role} value={role}>
                          {role}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>

                <Grid item xs={12}>
                  <Typography variant="h6" sx={{ mt: 3 }}>Account Status</Typography>
                  <Button
                    variant="contained"
                    color={selectedUser.isBlocked ? 'success' : 'error'}
                    onClick={() => handleToggleBlock(selectedUser.id, selectedUser.isBlocked)}
                    startIcon={selectedUser.isBlocked ? <UnblockIcon /> : <BlockIcon />}
                    sx={{ mt: 1 }}
                  >
                    {selectedUser.isBlocked ? 'Unblock User' : 'Block User'}
                  </Button>
                </Grid>
              </Grid>
            </DialogContent>
            <DialogActions>
              <Button onClick={handleCloseDialog}>Close</Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </Box>
  );
};

export default UsersManager;