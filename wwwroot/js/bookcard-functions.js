/**
 * BookCard Functions - JavaScript for Book Card Components
 * File: wwwroot/js/bookcard-functions.js
 * Các chức năng: Add to Cart, Quick View, Add to Wishlist
 */

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Lấy CSRF Token từ form
 */
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

/**
 * Hiển thị thông báo toast
 */
/**
 * Hiển thị notification không bị header che
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại: 'success', 'error', 'warning', 'info'
 * @param {number} duration - Thời gian hiển thị (ms)
 */
function showNotification(message, type = 'success', duration = 3000) {
    // Xóa notification cũ nếu có
    const oldNotification = document.querySelector('.toast-notification');
    if (oldNotification) {
        oldNotification.remove();
    }

    // Tính toán vị trí top dựa trên header height
    const header = document.querySelector('header, nav, .navbar');
    let topPosition = '1rem'; // 16px mặc định
    
    if (header) {
        const headerHeight = header.offsetHeight;
        // Thêm 16px padding bên dưới header
        topPosition = `${headerHeight + 16}px`;
    } else {
        // Nếu không tìm thấy header, dùng top cao hơn
        topPosition = '5rem'; // 80px
    }

    // Tạo notification mới
    const notification = document.createElement('div');
    notification.className = 'toast-notification fixed right-4 z-[10000] transform transition-all duration-300 ease-in-out';
    notification.style.top = topPosition;
    notification.style.transform = 'translateX(400px)'; // Start off-screen
    notification.style.opacity = '0';

    const bgColor = {
        'success': 'bg-green-500',
        'error': 'bg-red-500',
        'warning': 'bg-yellow-500',
        'info': 'bg-blue-500'
    }[type] || 'bg-gray-500';

    const icon = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    }[type] || 'fa-info-circle';

    notification.innerHTML = `
        <div class="${bgColor} text-white px-6 py-4 rounded-lg shadow-2xl flex items-center space-x-3 min-w-[300px] max-w-[500px]">
            <i class="fas ${icon} text-xl flex-shrink-0"></i>
            <span class="font-medium flex-grow">${message}</span>
            <button onclick="closeNotification(this)" 
                    class="ml-4 hover:bg-white/20 rounded p-1 flex-shrink-0 transition-colors">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    document.body.appendChild(notification);

    // Trigger animation (slide in)
    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
        notification.style.opacity = '1';
    }, 10);

    // Tự động xóa sau duration
    setTimeout(() => {
        closeNotificationElement(notification);
    }, duration);
}

/**
 * Đóng notification khi click nút X
 */
function closeNotification(button) {
    const notification = button.closest('.toast-notification');
    if (notification) {
        closeNotificationElement(notification);
    }
}

/**
 * Animation đóng notification
 */
function closeNotificationElement(notification) {
    notification.style.opacity = '0';
    notification.style.transform = 'translateX(400px)';
    setTimeout(() => {
        notification.remove();
    }, 300);
}

/**
 * Hiển thị nhiều notifications xếp chồng (nếu cần)
 * @param {string} message - Nội dung thông báo
 * @param {string} type - Loại thông báo
 * @param {number} duration - Thời gian hiển thị
 */
function showNotificationStack(message, type = 'success', duration = 3000) {
    // Tính toán vị trí top cho notification mới
    const header = document.querySelector('header, nav, .navbar');
    let baseTop = header ? header.offsetHeight + 16 : 80;
    
    // Đếm số notification đang hiển thị
    const existingNotifications = document.querySelectorAll('.toast-notification-stack');
    const offset = existingNotifications.length * 90; // Mỗi notification cách nhau 90px
    
    const topPosition = `${baseTop + offset}px`;

    // Tạo notification
    const notification = document.createElement('div');
    notification.className = 'toast-notification-stack fixed right-4 z-[10000] transform transition-all duration-300 ease-in-out';
    notification.style.top = topPosition;
    notification.style.transform = 'translateX(400px)';
    notification.style.opacity = '0';

    const bgColor = {
        'success': 'bg-green-500',
        'error': 'bg-red-500',
        'warning': 'bg-yellow-500',
        'info': 'bg-blue-500'
    }[type] || 'bg-gray-500';

    const icon = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    }[type] || 'fa-info-circle';

    notification.innerHTML = `
        <div class="${bgColor} text-white px-6 py-4 rounded-lg shadow-2xl flex items-center space-x-3 min-w-[300px] max-w-[500px] top-20">
            <i class="fas ${icon} text-xl flex-shrink-0"></i>
            <span class="font-medium flex-grow">${message}</span>
            <button onclick="closeNotificationStack(this)" 
                    class="ml-4 hover:bg-white/20 rounded p-1 flex-shrink-0 transition-colors">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    document.body.appendChild(notification);

    // Trigger animation
    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
        notification.style.opacity = '1';
    }, 10);

    // Tự động xóa và dời các notification khác lên
    setTimeout(() => {
        closeAndReposition(notification);
    }, duration);
}

/**
 * Đóng notification trong stack
 */
function closeNotificationStack(button) {
    const notification = button.closest('.toast-notification-stack');
    if (notification) {
        closeAndReposition(notification);
    }
}

/**
 * Đóng và sắp xếp lại vị trí các notifications
 */
function closeAndReposition(notification) {
    notification.style.opacity = '0';
    notification.style.transform = 'translateX(400px)';
    
    setTimeout(() => {
        notification.remove();
        
        // Dời các notification phía dưới lên
        const remainingNotifications = document.querySelectorAll('.toast-notification-stack');
        const header = document.querySelector('header, nav, .navbar');
        const baseTop = header ? header.offsetHeight + 16 : 80;
        
        remainingNotifications.forEach((notif, index) => {
            const newTop = `${baseTop + (index * 90)}px`;
            notif.style.top = newTop;
        });
    }, 300);
}

// Thêm CSS cho responsive trên mobile
const notificationStyles = document.createElement('style');
notificationStyles.textContent = `
    @media (max-width: 640px) {
        .toast-notification,
        .toast-notification-stack {
            right: 0.5rem !important;
            left: 0.5rem !important;
            width: calc(100% - 1rem) !important;
        }
        
        .toast-notification > div,
        .toast-notification-stack > div {
            min-width: 100% !important;
            max-width: 100% !important;
        }
    }
    
    /* Đảm bảo notification luôn nằm trên mọi element */
    .toast-notification,
    .toast-notification-stack {
        pointer-events: none;
    }
    
    .toast-notification > div,
    .toast-notification-stack > div {
        pointer-events: auto;
    }
`;
document.head.appendChild(notificationStyles);

/**
 * Cập nhật số lượng sản phẩm trong giỏ hàng (header)
 */
function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll('.cart-count, [data-cart-count], #cart-count');
    cartCountElements.forEach(element => {
        element.textContent = count;
        
        // Animation bounce
        element.classList.add('animate-bounce');
        setTimeout(() => {
            element.classList.remove('animate-bounce');
        }, 500);
    });
}

/**
 * Cập nhật số lượng wishlist (header)
 */
function updateWishlistCount(count) {
    const wishlistCountElements = document.querySelectorAll('.wishlist-count, [data-wishlist-count]');
    wishlistCountElements.forEach(element => {
        element.textContent = count;
        
        // Animation pulse
        element.classList.add('animate-pulse');
        setTimeout(() => {
            element.classList.remove('animate-pulse');
        }, 500);
    });
}

// ============================================
// ADD TO CART FUNCTION
// ============================================

/**
 * Thêm sách vào giỏ hàng
 * @param {number} bookId - ID của sách
 * @param {number} quantity - Số lượng (mặc định là 1)
 */
function addToCart(bookId, quantity = 1) {
    // Kiểm tra bookId hợp lệ
    if (!bookId || isNaN(bookId)) {
        showNotification('Thông tin sản phẩm không hợp lệ!', 'error');
        return;
    }

    // Disable button tạm thời để tránh double click
    const button = event?.target?.closest('button');
    if (button) {
        button.disabled = true;
        button.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Đang thêm...';
    }

    // Gửi request
    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({
            bookId: parseInt(bookId),
            quantity: parseInt(quantity)
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
            // Hiển thị thông báo thành công
            showNotification(data.message || 'Đã thêm vào giỏ hàng!', 'success');
            
            // Cập nhật số lượng giỏ hàng
            if (data.cartItemCount !== undefined) {
                updateCartCount(data.cartItemCount);
            }

            // Animation cho button
            if (button) {
                button.classList.add('animate-pulse');
                setTimeout(() => {
                    button.classList.remove('animate-pulse');
                }, 500);
            }
        } else {
            showNotification(data.message || 'Không thể thêm vào giỏ hàng!', 'error');
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        showNotification('Có lỗi xảy ra! Vui lòng thử lại.', 'error');
    })
    .finally(() => {
        // Re-enable button
        if (button) {
            button.disabled = false;
            button.innerHTML = '<i class="fas fa-shopping-cart mr-2"></i>Thêm vào giỏ hàng';
        }
    });
}

// ============================================
// ADD TO WISHLIST FUNCTION
// ============================================

/**
 * Thêm/xóa sách khỏi danh sách yêu thích
 * @param {number} bookId - ID của sách
 */
function addToWishlist(bookId) {
    // Kiểm tra bookId hợp lệ
    if (!bookId || isNaN(bookId)) {
        showNotification('Thông tin sản phẩm không hợp lệ!', 'error');
        return;
    }

    // Get button element
    const button = event?.target?.closest('button');
    if (button) {
        button.disabled = true;
    }

    // Gửi request
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({
            bookId: parseInt(bookId)
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
            const isInWishlist = data.isInWishlist;
            
            // Cập nhật icon
            if (button) {
                const icon = button.querySelector('i');
                if (icon) {
                    if (isInWishlist) {
                        icon.className = 'fas fa-heart text-pink-500';
                        button.classList.add('text-pink-500');
                    } else {
                        icon.className = 'far fa-heart';
                        button.classList.remove('text-pink-500');
                    }
                }
            }

            // Hiển thị thông báo
            const message = isInWishlist 
                ? 'Đã thêm vào yêu thích!' 
                : 'Đã xóa khỏi yêu thích!';
            showNotification(message, 'success');

            // Cập nhật wishlist count nếu có
            if (data.wishlistCount !== undefined) {
                updateWishlistCount(data.wishlistCount);
            }
        } else {
            showNotification(data.message || 'Có lỗi xảy ra!', 'error');
        }
    })
    .catch(error => {
        console.error('Error toggling wishlist:', error);
        showNotification('Có lỗi xảy ra! Vui lòng thử lại.', 'error');
    })
    .finally(() => {
        if (button) {
            button.disabled = false;
        }
    });
}

// ============================================
// QUICK VIEW FUNCTION
// ============================================

/**
 * Xem nhanh thông tin sách (modal hoặc redirect)
 * @param {number} bookId - ID của sách
 */
function quickView(bookId) {
    // Kiểm tra bookId hợp lệ
    if (!bookId || isNaN(bookId)) {
        showNotification('Thông tin sản phẩm không hợp lệ!', 'error');
        return;
    }

    // Option 1: Mở modal quickview (nếu có)
    const quickViewModal = document.getElementById('quickViewModal');
    if (quickViewModal) {
        openQuickViewModal(bookId);
        return;
    }

    // Option 2: Redirect đến trang chi tiết
    window.location.href = `/Books/Details/${bookId}`;
}

/**
 * Mở modal quick view (nếu có)
 */
function openQuickViewModal(bookId) {
    const modal = document.getElementById('quickViewModal');
    const modalBody = modal?.querySelector('.modal-body');
    
    if (!modal || !modalBody) {
        // Fallback: redirect
        window.location.href = `/Books/Details/${bookId}`;
        return;
    }

    // Show modal
    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';

    // Show loading
    modalBody.innerHTML = `
        <div class="flex items-center justify-center py-12">
            <i class="fas fa-spinner fa-spin text-4xl text-blue-500"></i>
            <span class="ml-3 text-gray-600">Đang tải...</span>
        </div>
    `;

    // Load book details via AJAX
    fetch(`/Books/QuickView/${bookId}`)
        .then(response => response.text())
        .then(html => {
            modalBody.innerHTML = html;
        })
        .catch(error => {
            console.error('Error loading quick view:', error);
            modalBody.innerHTML = `
                <div class="text-center py-12">
                    <i class="fas fa-exclamation-circle text-4xl text-red-500 mb-4"></i>
                    <p class="text-gray-600">Không thể tải thông tin sản phẩm!</p>
                    <button onclick="closeQuickView()" class="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg">
                        Đóng
                    </button>
                </div>
            `;
        });
}

/**
 * Đóng modal quick view
 */
function closeQuickView() {
    const modal = document.getElementById('quickViewModal');
    if (modal) {
        modal.classList.add('hidden');
        document.body.style.overflow = '';
    }
}

// ============================================
// INITIALIZATION
// ============================================

/**
 * Khởi tạo khi DOM loaded
 */
document.addEventListener('DOMContentLoaded', function() {
    console.log('BookCard functions loaded successfully!');

    // Close modal on ESC key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeQuickView();
        }
    });

    // Close modal on backdrop click
    const quickViewModal = document.getElementById('quickViewModal');
    if (quickViewModal) {
        quickViewModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closeQuickView();
            }
        });
    }
});

// ============================================
// ANIMATION STYLES (Add to CSS)
// ============================================
/*
Add these styles to your CSS file:

@keyframes slide-in {
    from {
        opacity: 0;
        transform: translateX(100%);
    }
    to {
        opacity: 1;
        transform: translateX(0);
    }
}

.animate-slide-in {
    animation: slide-in 0.3s ease-out;
}
*/