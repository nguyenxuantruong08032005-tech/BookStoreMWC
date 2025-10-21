/**
 * cart-functions.js - Common functions for cart operations
 * Các hàm dùng chung cho giỏ hàng
 */

/**
 * Hiển thị thông báo toast
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại: 'success', 'error', 'info', 'warning'
 */
function showNotification(message, type) {
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 px-6 py-3 rounded-lg text-white z-50 shadow-lg transition-all ${
        type === 'success' ? 'bg-green-500' :
        type === 'error' ? 'bg-red-500' :
        type === 'info' ? 'bg-blue-500' :
        type === 'warning' ? 'bg-yellow-500' : 'bg-gray-500'
    }`;
    notification.textContent = message;
    notification.style.animation = 'slideInRight 0.3s ease';
    
    document.body.appendChild(notification);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        notification.style.opacity = '0';
        notification.style.transform = 'translateX(400px)';
        notification.style.transition = 'all 0.3s ease';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

/**
 * Cập nhật số lượng items trong giỏ hàng
 * @param {number} count - Số lượng items
 */
function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll('.cart-count, [class*="badge"]');
    cartCountElements.forEach(element => {
        element.textContent = count;
        // Add animation
        element.style.transform = 'scale(1.3)';
        setTimeout(() => {
            element.style.transform = 'scale(1)';
        }, 200);
    });
}

/**
 * Thêm sản phẩm vào giỏ hàng
 * @param {number} bookId - ID của sách
 * @param {number} quantity - Số lượng (mặc định = 1)
 */
function addToCart(bookId, quantity = 1) {
    // Lấy anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    if (!token) {
        console.warn('Anti-forgery token not found!');
    }
    
    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({
            bookId: bookId,
            quantity: quantity
        })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            showNotification(data.message || 'Đã thêm vào giỏ hàng!', 'success');
            
            // Cập nhật số lượng giỏ hàng
            if (data.cartItemCount !== undefined) {
                updateCartCount(data.cartItemCount);
            }
        } else {
            showNotification(data.message || 'Không thể thêm vào giỏ hàng!', 'error');
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        showNotification('Đã xảy ra lỗi khi thêm vào giỏ hàng!', 'error');
    });
}

/**
 * Cập nhật số lượng sản phẩm trong giỏ hàng
 * @param {number} bookId - ID của sách
 * @param {number} quantity - Số lượng mới
 */
function updateCartQuantity(bookId, quantity) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    fetch('/Cart/UpdateCartItem', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({
            bookId: bookId,
            quantity: quantity
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification('Đã cập nhật giỏ hàng!', 'success');
            
            // Cập nhật UI nếu cần
            if (data.cart) {
                updateCartCount(data.cart.itemCount);
            }
        } else {
            showNotification(data.message || 'Không thể cập nhật!', 'error');
        }
    })
    .catch(error => {
        console.error('Error updating cart:', error);
        showNotification('Đã xảy ra lỗi!', 'error');
    });
}

/**
 * Xóa sản phẩm khỏi giỏ hàng
 * @param {number} bookId - ID của sách
 */
function removeFromCart(bookId) {
    if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) {
        return;
    }
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    fetch('/Cart/RemoveFromCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({
            bookId: bookId
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification('Đã xóa khỏi giỏ hàng!', 'success');
            
            // Reload trang hoặc xóa element
            setTimeout(() => {
                location.reload();
            }, 500);
        } else {
            showNotification(data.message || 'Không thể xóa!', 'error');
        }
    })
    .catch(error => {
        console.error('Error removing from cart:', error);
        showNotification('Đã xảy ra lỗi!', 'error');
    });
}

// Add CSS animation
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;
document.head.appendChild(style);