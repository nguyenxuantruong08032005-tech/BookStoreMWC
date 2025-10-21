/**
 * Admin Panel JavaScript - HOÀN CHỈNH & ĐÃ SỬA TẤT CẢ LỖI
 * Phiên bản đầy đủ với tất cả chức năng
 */

class AdminPanel {
    constructor() {
        this.searchTimeout = null;
        this.notificationInterval = null;
        this.charts = {};
        this.init();
    }

    init() {
        this.initSearch();
        // this.initNotifications(); // Disabled - notification API not working
        this.initDataTables();
        this.initFormValidation();
        this.initFileUploads();
        this.initCharts();
        this.initTooltips();
        this.initModalHandlers();
    }

    /**
     * Admin Search with Autocomplete - ĐÃ SỬA
     */
    initSearch() {
        const searchInput = $('#admin-search');
        const suggestionsBox = $('#search-suggestions');
        
        if (!searchInput.length) return;

        searchInput.on('input', (e) => {
            const query = e.target.value.trim();
            
            if (this.searchTimeout) {
                clearTimeout(this.searchTimeout);
            }

            if (query.length < 2) {
                suggestionsBox.addClass('hidden').empty();
                return;
            }

            this.searchTimeout = setTimeout(() => {
                this.performSearch(query);
            }, 300);
        });

        $(document).on('click', (e) => {
            if (!$(e.target).closest('#admin-search, #search-suggestions').length) {
                suggestionsBox.addClass('hidden');
            }
        });

        searchInput.on('keydown', (e) => {
            if (e.key === 'Escape') {
                suggestionsBox.addClass('hidden');
            }
        });
    }

    performSearch(query) {
        const suggestionsBox = $('#search-suggestions');
        
        suggestionsBox.removeClass('hidden').html(`
            <div class="p-4 text-center">
                <i class="fas fa-spinner fa-spin text-gray-400 mr-2"></i>
                <span class="text-sm text-gray-600">Đang tìm kiếm...</span>
            </div>
        `);

        $.ajax({
            url: '/Admin/Search',
            method: 'GET',
            data: { q: query },
            success: (response) => {
                this.displaySearchResults(response);
            },
            error: () => {
                suggestionsBox.html(`
                    <div class="p-4 text-center text-red-600">
                        <i class="fas fa-exclamation-circle mr-2"></i>
                        <span class="text-sm">Lỗi khi tìm kiếm</span>
                    </div>
                `);
            }
        });
    }

    displaySearchResults(results) {
        const suggestionsBox = $('#search-suggestions');
        
        if (!results || (!results.books?.length && !results.orders?.length && !results.users?.length)) {
            suggestionsBox.html(`
                <div class="p-4 text-center text-gray-500">
                    <i class="fas fa-search text-2xl mb-2"></i>
                    <p class="text-sm">Không tìm thấy kết quả</p>
                </div>
            `);
            return;
        }

        let html = '<div class="py-2 max-h-96 overflow-y-auto">';
        
        if (results.books?.length) {
            html += '<div class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase">Sách</div>';
            results.books.forEach(book => {
                html += `
                    <a href="/Admin/Books/Edit/${book.id}" class="block px-4 py-2 hover:bg-gray-50 transition-colors">
                        <div class="flex items-center">
                            <i class="fas fa-book text-blue-500 mr-3"></i>
                            <div class="flex-1">
                                <p class="text-sm font-medium text-gray-900">${book.title}</p>
                                <p class="text-xs text-gray-500">${book.author}</p>
                            </div>
                            <span class="text-sm font-semibold text-primary-600">${this.formatCurrency(book.price)}</span>
                        </div>
                    </a>
                `;
            });
        }
        
        if (results.orders?.length) {
            html += '<div class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase border-t mt-2">Đơn hàng</div>';
            results.orders.forEach(order => {
                html += `
                    <a href="/Admin/Orders/Details/${order.id}" class="block px-4 py-2 hover:bg-gray-50 transition-colors">
                        <div class="flex items-center">
                            <i class="fas fa-shopping-cart text-green-500 mr-3"></i>
                            <div class="flex-1">
                                <p class="text-sm font-medium text-gray-900">Đơn #${order.id}</p>
                                <p class="text-xs text-gray-500">${order.customerName}</p>
                            </div>
                        </div>
                    </a>
                `;
            });
        }
        
        if (results.users?.length) {
            html += '<div class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase border-t mt-2">Người dùng</div>';
            results.users.forEach(user => {
                html += `
                    <a href="/Admin/Users/Details/${user.id}" class="block px-4 py-2 hover:bg-gray-50 transition-colors">
                        <div class="flex items-center">
                            <i class="fas fa-user text-purple-500 mr-3"></i>
                            <div class="flex-1">
                                <p class="text-sm font-medium text-gray-900">${user.name}</p>
                                <p class="text-xs text-gray-500">${user.email}</p>
                            </div>
                        </div>
                    </a>
                `;
            });
        }
        
        html += '</div>';
        suggestionsBox.html(html);
    }

    /**
     * Notifications System - ĐÃ SỬA
     */
    initNotifications() {
        this.loadNotifications();
        
        // Tự động tải lại mỗi 60 giây
        this.notificationInterval = setInterval(() => {
            this.loadNotifications();
        }, 60000);
        
        // Click notification icon
        $('#notifications-btn').on('click', (e) => {
            e.stopPropagation();
            $('#notifications-dropdown').toggleClass('hidden');
            this.markNotificationsAsRead();
        });
    }

    loadNotifications() {
        $.ajax({
            url: '/Admin/Notifications/GetUnread',
            method: 'GET',
            success: (response) => {
                if (response.success) {
                    this.updateNotificationBadge(response.count);
                    this.renderNotifications(response.notifications);
                }
            },
            error: (xhr) => {
                console.error('Error loading notifications:', xhr);
            }
        });
    }

    updateNotificationBadge(count) {
        const badge = $('#notification-badge');
        if (count > 0) {
            badge.text(count > 99 ? '99+' : count).removeClass('hidden');
        } else {
            badge.addClass('hidden');
        }
    }

    renderNotifications(notifications) {
        const container = $('#notifications-list');
        
        if (!notifications || notifications.length === 0) {
            container.html(`
                <div class="p-4 text-center text-gray-500">
                    <i class="fas fa-bell-slash text-2xl mb-2"></i>
                    <p class="text-sm">Không có thông báo mới</p>
                </div>
            `);
            return;
        }

        let html = '';
        notifications.forEach(notif => {
            const iconClass = this.getNotificationIcon(notif.type);
            const colorClass = this.getNotificationColor(notif.type);
            
            html += `
                <a href="${notif.url}" class="block px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-100 last:border-b-0">
                    <div class="flex items-start">
                        <div class="flex-shrink-0">
                            <div class="w-10 h-10 ${colorClass} rounded-full flex items-center justify-center">
                                <i class="${iconClass} text-white"></i>
                            </div>
                        </div>
                        <div class="ml-3 flex-1">
                            <p class="text-sm font-medium text-gray-900">${notif.title}</p>
                            <p class="text-xs text-gray-500 mt-1">${notif.message}</p>
                            <p class="text-xs text-gray-400 mt-1">${this.formatTimeAgo(notif.createdAt)}</p>
                        </div>
                    </div>
                </a>
            `;
        });

        container.html(html);
    }

    getNotificationIcon(type) {
        const icons = {
            'order': 'fas fa-shopping-cart',
            'user': 'fas fa-user-plus',
            'review': 'fas fa-star',
            'stock': 'fas fa-exclamation-triangle',
            'system': 'fas fa-cog'
        };
        return icons[type] || 'fas fa-bell';
    }

    getNotificationColor(type) {
        const colors = {
            'order': 'bg-green-500',
            'user': 'bg-blue-500',
            'review': 'bg-yellow-500',
            'stock': 'bg-red-500',
            'system': 'bg-gray-500'
        };
        return colors[type] || 'bg-gray-500';
    }

    markNotificationsAsRead() {
        $.ajax({
            url: '/Admin/Notifications/MarkAsRead',
            method: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: () => {
                this.updateNotificationBadge(0);
            }
        });
    }

    /**
     * DataTables Initialization - ĐÃ SỬA
     */
    initDataTables() {
        if (typeof $.fn.DataTable === 'undefined') {
            console.warn('DataTables not loaded');
            return;
        }

        $('.data-table').each(function() {
            const $table = $(this);
            const options = {
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/vi.json'
                },
                responsive: true,
                pageLength: 25,
                order: [[0, 'desc']],
                dom: '<"flex flex-col md:flex-row md:items-center md:justify-between mb-4"<"mb-2 md:mb-0"l><"mb-2 md:mb-0"f>>rtip'
            };

            if ($table.data('order-column')) {
                options.order = [[$table.data('order-column'), $table.data('order-dir') || 'asc']];
            }

            $table.DataTable(options);
        });
    }

    /**
     * Form Validation - ĐÃ SỬA
     */
    initFormValidation() {
        $('form[data-ajax="true"]').on('submit', function(e) {
            e.preventDefault();
            
            const $form = $(this);
            const url = $form.attr('action');
            const method = $form.attr('method') || 'POST';
            const formData = new FormData(this);

            $.ajax({
                url: url,
                method: method,
                data: formData,
                processData: false,
                contentType: false,
                beforeSend: () => {
                    $form.find('button[type="submit"]').prop('disabled', true);
                    showLoading('Đang xử lý...');
                },
                success: (response) => {
                    hideLoading();
                    
                    if (response.success) {
                        showNotification('success', response.message || 'Thành công!');
                        
                        if (response.redirectUrl) {
                            setTimeout(() => {
                                window.location.href = response.redirectUrl;
                            }, 1000);
                        } else if (response.reload) {
                            setTimeout(() => {
                                window.location.reload();
                            }, 1000);
                        }
                    } else {
                        showNotification('error', response.message || 'Có lỗi xảy ra!');
                    }
                },
                error: (xhr) => {
                    hideLoading();
                    console.error('Form submission error:', xhr);
                    showNotification('error', 'Đã xảy ra lỗi khi gửi form!');
                },
                complete: () => {
                    $form.find('button[type="submit"]').prop('disabled', false);
                }
            });
        });

        // Real-time validation
        $('input[required], textarea[required], select[required]').on('blur', function() {
            const $field = $(this);
            const value = $field.val();

            if (!value || value.trim() === '') {
                $field.addClass('border-red-500');
                $field.parent().find('.error-message').remove();
                $field.parent().append('<p class="error-message text-red-500 text-xs mt-1">Trường này không được để trống</p>');
            } else {
                $field.removeClass('border-red-500');
                $field.parent().find('.error-message').remove();
            }
        });

        // Email validation
        $('input[type="email"]').on('blur', function() {
            const $field = $(this);
            const email = $field.val();
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (email && !emailRegex.test(email)) {
                $field.addClass('border-red-500');
                $field.parent().find('.error-message').remove();
                $field.parent().append('<p class="error-message text-red-500 text-xs mt-1">Email không hợp lệ</p>');
            }
        });

        // Number validation
        $('input[type="number"]').on('input', function() {
            const $field = $(this);
            const min = parseFloat($field.attr('min'));
            const max = parseFloat($field.attr('max'));
            const value = parseFloat($field.val());

            if (!isNaN(min) && value < min) {
                $field.val(min);
            }
            if (!isNaN(max) && value > max) {
                $field.val(max);
            }
        });
    }

    /**
     * File Upload Handler - ĐÃ SỬA
     */
    initFileUploads() {
        $('input[type="file"]').on('change', function() {
            const $input = $(this);
            const files = this.files;
            const $preview = $input.closest('.file-upload-container').find('.file-preview');

            if (!files || files.length === 0) return;

            $preview.empty();

            Array.from(files).forEach((file, index) => {
                if (file.type.startsWith('image/')) {
                    const reader = new FileReader();
                    
                    reader.onload = (e) => {
                        const html = `
                            <div class="relative inline-block mr-2 mb-2">
                                <img src="${e.target.result}" alt="${file.name}" class="w-24 h-24 object-cover rounded-lg border-2 border-gray-300">
                                <button type="button" class="remove-file absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center hover:bg-red-600" data-index="${index}">
                                    <i class="fas fa-times text-xs"></i>
                                </button>
                                <p class="text-xs text-gray-600 mt-1 text-center truncate max-w-24">${file.name}</p>
                            </div>
                        `;
                        $preview.append(html);
                    };
                    
                    reader.readAsDataURL(file);
                } else {
                    const html = `
                        <div class="relative inline-block mr-2 mb-2">
                            <div class="w-24 h-24 bg-gray-100 rounded-lg border-2 border-gray-300 flex items-center justify-center">
                                <i class="fas fa-file text-3xl text-gray-400"></i>
                            </div>
                            <button type="button" class="remove-file absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center hover:bg-red-600" data-index="${index}">
                                <i class="fas fa-times text-xs"></i>
                            </button>
                            <p class="text-xs text-gray-600 mt-1 text-center truncate max-w-24">${file.name}</p>
                        </div>
                    `;
                    $preview.append(html);
                }
            });
        });

        // Remove file handler
        $(document).on('click', '.remove-file', function() {
            const $btn = $(this);
            const $container = $btn.closest('.file-upload-container');
            const $input = $container.find('input[type="file"]');
            
            $btn.parent().remove();
            
            // Reset input if no files left
            if ($container.find('.file-preview').children().length === 0) {
                $input.val('');
            }
        });
    }

    /**
     * Charts Initialization - ĐÃ HOÀN CHỈNH
     */
    initCharts() {
        // Revenue Chart
        if ($('#revenueChart').length) {
            this.initRevenueChart();
        }

        // Orders Status Chart
        if ($('#ordersStatusChart').length) {
            this.initOrdersStatusChart();
        }

        // Books Category Chart
        if ($('#booksCategoryChart').length) {
            this.initBooksCategoryChart();
        }

        // User Registrations Chart
        if ($('#userRegistrationsChart').length) {
            this.initUserRegistrationsChart();
        }
    }

    initRevenueChart() {
        const ctx = document.getElementById('revenueChart');
        if (!ctx) return;

        const data = JSON.parse(ctx.dataset.chartData || '{}');
        
        this.charts.revenue = new Chart(ctx, {
            type: 'line',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Doanh thu',
                    data: Object.values(data),
                    borderColor: 'rgb(59, 130, 246)',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => {
                                return 'Doanh thu: ' + this.formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: (value) => {
                                return this.formatCurrency(value);
                            }
                        }
                    }
                }
            }
        });
    }

    initOrdersStatusChart() {
        const ctx = document.getElementById('ordersStatusChart');
        if (!ctx) return;

        const data = JSON.parse(ctx.dataset.chartData || '{}');
        
        this.charts.ordersStatus = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    data: Object.values(data),
                    backgroundColor: [
                        'rgb(251, 191, 36)',
                        'rgb(59, 130, 246)',
                        'rgb(16, 185, 129)',
                        'rgb(239, 68, 68)',
                        'rgb(156, 163, 175)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    initBooksCategoryChart() {
        const ctx = document.getElementById('booksCategoryChart');
        if (!ctx) return;

        const data = JSON.parse(ctx.dataset.chartData || '{}');
        
        this.charts.booksCategory = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Số lượng sách',
                    data: Object.values(data),
                    backgroundColor: 'rgb(139, 92, 246)'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }

    initUserRegistrationsChart() {
        const ctx = document.getElementById('userRegistrationsChart');
        if (!ctx) return;

        const data = JSON.parse(ctx.dataset.chartData || '{}');
        
        this.charts.userRegistrations = new Chart(ctx, {
            type: 'line',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Đăng ký mới',
                    data: Object.values(data),
                    borderColor: 'rgb(16, 185, 129)',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }

    /**
     * Tooltips - ĐÃ SỬA
     */
    initTooltips() {
        $('[data-tooltip]').each(function() {
            const $element = $(this);
            const text = $element.data('tooltip');
            const position = $element.data('tooltip-position') || 'top';

            $element.on('mouseenter', function() {
                const tooltip = $(`
                    <div class="tooltip absolute z-50 px-2 py-1 text-xs text-white bg-gray-900 rounded shadow-lg whitespace-nowrap">
                        ${text}
                    </div>
                `);

                $('body').append(tooltip);

                const offset = $element.offset();
                const elementWidth = $element.outerWidth();
                const elementHeight = $element.outerHeight();
                const tooltipWidth = tooltip.outerWidth();
                const tooltipHeight = tooltip.outerHeight();

                let top, left;

                switch(position) {
                    case 'top':
                        top = offset.top - tooltipHeight - 8;
                        left = offset.left + (elementWidth / 2) - (tooltipWidth / 2);
                        break;
                    case 'bottom':
                        top = offset.top + elementHeight + 8;
                        left = offset.left + (elementWidth / 2) - (tooltipWidth / 2);
                        break;
                    case 'left':
                        top = offset.top + (elementHeight / 2) - (tooltipHeight / 2);
                        left = offset.left - tooltipWidth - 8;
                        break;
                    case 'right':
                        top = offset.top + (elementHeight / 2) - (tooltipHeight / 2);
                        left = offset.left + elementWidth + 8;
                        break;
                }

                tooltip.css({ top: top, left: left });
            });

            $element.on('mouseleave', function() {
                $('.tooltip').remove();
            });
        });
    }

    /**
     * Modal Handlers - MỚI THÊM
     */
    initModalHandlers() {
        // Open modal
        $('[data-modal-target]').on('click', function() {
            const target = $(this).data('modal-target');
            $(target).removeClass('hidden');
            $('body').addClass('overflow-hidden');
        });

        // Close modal
        $('[data-modal-close]').on('click', function() {
            const modal = $(this).closest('.modal');
            modal.addClass('hidden');
            $('body').removeClass('overflow-hidden');
        });

        // Close on backdrop click
        $('.modal').on('click', function(e) {
            if ($(e.target).hasClass('modal')) {
                $(this).addClass('hidden');
                $('body').removeClass('overflow-hidden');
            }
        });

        // Close on ESC key
        $(document).on('keydown', function(e) {
            if (e.key === 'Escape') {
                $('.modal:not(.hidden)').addClass('hidden');
                $('body').removeClass('overflow-hidden');
            }
        });
    }

    /**
     * Utility Functions
     */
    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    formatNumber(num) {
        return new Intl.NumberFormat('vi-VN').format(num);
    }

    formatTimeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now - date) / 1000);

        if (seconds < 60) return 'Vừa xong';
        if (seconds < 3600) return Math.floor(seconds / 60) + ' phút trước';
        if (seconds < 86400) return Math.floor(seconds / 3600) + ' giờ trước';
        if (seconds < 604800) return Math.floor(seconds / 86400) + ' ngày trước';
        
        return date.toLocaleDateString('vi-VN');
    }

    destroy() {
        if (this.notificationInterval) {
            clearInterval(this.notificationInterval);
        }
        
        Object.values(this.charts).forEach(chart => {
            if (chart && typeof chart.destroy === 'function') {
                chart.destroy();
            }
        });
    }
}

// ============================================
// GLOBAL FUNCTIONS
// ============================================

function showNotification(type, message, duration = 5000) {
    const icons = {
        success: 'fa-check-circle',
        error: 'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle',
        info: 'fa-info-circle'
    };

    const colors = {
        success: 'bg-green-500',
        error: 'bg-red-500',
        warning: 'bg-yellow-500',
        info: 'bg-blue-500'
    };

    const notification = $(`
        <div class="fixed top-4 right-4 z-50 ${colors[type]} text-white px-6 py-4 rounded-lg shadow-lg flex items-center space-x-3 animate-slide-in">
            <i class="fas ${icons[type]} text-xl"></i>
            <span class="font-medium">${message}</span>
            <button class="ml-4 hover:bg-white hover:bg-opacity-20 rounded p-1">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);
    
    $('body').append(notification);
    
    notification.find('button').on('click', function() {
        notification.fadeOut(300, function() {
            $(this).remove();
        });
    });
    
    setTimeout(() => {
        notification.fadeOut(300, function() {
            $(this).remove();
        });
    }, duration);
}

function confirmAction(message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

function showLoading(text = 'Đang tải...') {
    const spinner = $('#loadingSpinner');
    if (spinner.length) {
        spinner.find('#loadingText').text(text);
        spinner.removeClass('hidden');
    } else {
        // Create loading spinner if not exists
        const html = `
            <div id="loadingSpinner" class="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                <div class="bg-white rounded-lg p-6 flex flex-col items-center">
                    <i class="fas fa-spinner fa-spin text-4xl text-primary-600 mb-4"></i>
                    <p id="loadingText" class="text-gray-700 font-medium">${text}</p>
                </div>
            </div>
        `;
        $('body').append(html);
    }
}

function hideLoading() {
    $('#loadingSpinner').addClass('hidden');
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatNumber(num) {
    return new Intl.NumberFormat('vi-VN').format(num);
}

// ============================================
// ADMIN SPECIFIC FUNCTIONS
// ============================================

/**
 * Delete Book
 */
function deleteBook(bookId) {
    if (!confirm('Bạn có chắc muốn xóa sách này? Hành động này không thể hoàn tác.')) {
        return;
    }

    showLoading('Đang xóa sách...');

    $.ajax({
        url: `/Admin/Books/Delete/${bookId}`,
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', response.message || 'Xóa sách thành công!');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message || 'Không thể xóa sách!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Delete error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi khi xóa sách!');
        }
    });
}

/**
 * Delete Category
 */
function deleteCategory(categoryId) {
    if (!confirm('Bạn có chắc muốn xóa danh mục này? Hành động này không thể hoàn tác.')) {
        return;
    }

    showLoading('Đang xóa danh mục...');

    $.ajax({
        url: `/Admin/Categories/Delete/${categoryId}`,
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', response.message || 'Xóa danh mục thành công!');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message || 'Không thể xóa danh mục!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Delete error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi khi xóa danh mục!');
        }
    });
}

/**
 * Update Order Status
 */
function updateOrderStatus(orderId, newStatus) {
    showLoading('Đang cập nhật trạng thái...');

    $.ajax({
        url: `/Admin/Orders/UpdateStatus/${orderId}`,
        method: 'POST',
        data: { status: newStatus },
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', response.message || 'Cập nhật trạng thái thành công!');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message || 'Không thể cập nhật trạng thái!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Update error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi khi cập nhật!');
        }
    });
}

/**
 * Toggle User Lock
 */
function toggleUserLock(userId, currentStatus) {
    const action = currentStatus ? 'mở khóa' : 'khóa';
    
    if (!confirm(`Bạn có chắc muốn ${action} người dùng này?`)) {
        return;
    }

    showLoading(`Đang ${action} người dùng...`);

    $.ajax({
        url: `/Admin/Users/ToggleLock/${userId}`,
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', response.message);
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message || 'Không thể thực hiện thao tác!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Toggle lock error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi!');
        }
    });
}

/**
 * Delete Review
 */
function deleteReview(reviewId) {
    if (!confirm('Bạn có chắc muốn xóa đánh giá này?')) {
        return;
    }

    showLoading('Đang xóa đánh giá...');

    $.ajax({
        url: `/Admin/Reviews/Delete/${reviewId}`,
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', response.message || 'Xóa đánh giá thành công!');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                showNotification('error', response.message || 'Không thể xóa đánh giá!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Delete error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi!');
        }
    });
}

/**
 * Update Display Order (for categories)
 */
function updateDisplayOrder(categoryId, newOrder) {
    $.ajax({
        url: `/Admin/Categories/UpdateDisplayOrder`,
        method: 'POST',
        data: { 
            categoryId: categoryId, 
            displayOrder: newOrder 
        },
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            if (response.success) {
                showNotification('success', 'Cập nhật thứ tự hiển thị thành công!', 2000);
            }
        },
        error: (xhr) => {
            console.error('Update display order error:', xhr);
            showNotification('error', 'Không thể cập nhật thứ tự!');
        }
    });
}

/**
 * Toggle Category Status
 */
function toggleCategoryStatus(categoryId, currentStatus) {
    $.ajax({
        url: `/Admin/Categories/ToggleStatus`,
        method: 'POST',
        data: { 
            categoryId: categoryId,
            isActive: !currentStatus
        },
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            if (response.success) {
                showNotification('success', response.message, 2000);
            } else {
                showNotification('error', response.message);
                // Revert checkbox
                $(`#category-status-${categoryId}`).prop('checked', currentStatus);
            }
        },
        error: (xhr) => {
            console.error('Toggle status error:', xhr);
            showNotification('error', 'Không thể cập nhật trạng thái!');
            // Revert checkbox
            $(`#category-status-${categoryId}`).prop('checked', currentStatus);
        }
    });
}

/**
 * Export Categories
 */
function exportCategories() {
    showLoading('Đang xuất dữ liệu...');
    
    window.location.href = '/Admin/Categories/Export';
    
    setTimeout(() => {
        hideLoading();
    }, 2000);
}

/**
 * Export Books
 */
function exportBooks() {
    showLoading('Đang xuất dữ liệu...');
    
    window.location.href = '/Admin/Books/Export';
    
    setTimeout(() => {
        hideLoading();
    }, 2000);
}

/**
 * Export Orders
 */
function exportOrders(status = null) {
    showLoading('Đang xuất dữ liệu...');
    
    let url = '/Admin/Orders/Export';
    if (status) {
        url += `?status=${status}`;
    }
    
    window.location.href = url;
    
    setTimeout(() => {
        hideLoading();
    }, 2000);
}

/**
 * Export Report
 */
function exportReport(type = 'full', format = 'csv') {
    showLoading('Đang tạo báo cáo...');
    
    window.location.href = `/Admin/Reports/Export?type=${type}&format=${format}`;
    
    setTimeout(() => {
        hideLoading();
    }, 3000);
}

/**
 * Clear Cache
 */
function clearCache() {
    if (!confirm('Bạn có chắc muốn xóa cache hệ thống?')) {
        return;
    }

    showLoading('Đang xóa cache...');

    $.ajax({
        url: '/Admin/Settings/ClearCache',
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', 'Xóa cache thành công!');
            } else {
                showNotification('error', 'Không thể xóa cache!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Clear cache error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi!');
        }
    });
}

/**
 * Create Backup
 */
function createBackup() {
    if (!confirm('Bạn có muốn tạo bản sao lưu dữ liệu?')) {
        return;
    }

    showLoading('Đang tạo backup...');

    $.ajax({
        url: '/Admin/Settings/CreateBackup',
        method: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            hideLoading();
            if (response.success) {
                showNotification('success', 'Tạo backup thành công!');
            } else {
                showNotification('error', 'Không thể tạo backup!');
            }
        },
        error: (xhr) => {
            hideLoading();
            console.error('Create backup error:', xhr);
            showNotification('error', 'Đã xảy ra lỗi!');
        }
    });
}

/**
 * Preview Image before upload
 */
function previewImage(input, previewId) {
    const file = input.files[0];
    const preview = document.getElementById(previewId);
    
    if (file && preview) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            preview.src = e.target.result;
            preview.classList.remove('hidden');
        };
        
        reader.readAsDataURL(file);
    }
}

/**
 * Toggle Book Status
 */
function toggleBookStatus(bookId, currentStatus) {
    $.ajax({
        url: `/Admin/Books/ToggleStatus`,
        method: 'POST',
        data: { 
            bookId: bookId,
            isActive: !currentStatus
        },
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: (response) => {
            if (response.success) {
                showNotification('success', response.message, 2000);
            } else {
                showNotification('error', response.message);
                $(`#book-status-${bookId}`).prop('checked', currentStatus);
            }
        },
        error: (xhr) => {
            console.error('Toggle status error:', xhr);
            showNotification('error', 'Không thể cập nhật trạng thái!');
            $(`#book-status-${bookId}`).prop('checked', currentStatus);
        }
    });
}

// ============================================
// DOCUMENT READY
// ============================================

$(document).ready(function() {
    // Create global admin panel instance
    if (typeof AdminPanel !== 'undefined') {
        window.adminPanel = new AdminPanel();
    }

    // Mobile sidebar toggle
    $('#mobile-sidebar-toggle').on('click', function() {
        $('#sidebar').removeClass('-translate-x-full');
        $('#mobile-menu-overlay').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    });

    // Close mobile sidebar
    $('#mobile-sidebar-close, #mobile-menu-overlay').on('click', function() {
        $('#sidebar').addClass('-translate-x-full');
        $('#mobile-menu-overlay').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    });

    // Admin dropdown toggles
    $('.admin-dropdown-toggle').on('click', function() {
        const $content = $(this).next('.admin-dropdown-content');
        const $icon = $(this).find('i.fa-chevron-down');

        $content.slideToggle(200);
        $icon.toggleClass('rotate-180');

        // Close other dropdowns
        $('.admin-dropdown-content').not($content).slideUp(200);
        $('.admin-dropdown-toggle i.fa-chevron-down').not($icon).removeClass('rotate-180');
    });

    // Highlight current page in sidebar
    const currentPath = window.location.pathname.toLowerCase();
    $('aside nav a').each(function() {
        const href = $(this).attr('href');
        if (href && currentPath.includes(href.toLowerCase())) {
            $(this).addClass('bg-gray-800 text-white');

            const $dropdown = $(this).closest('.admin-dropdown-content');
            if ($dropdown.length) {
                $dropdown.show();
                $dropdown.prev('.admin-dropdown-toggle').find('i.fa-chevron-down').addClass('rotate-180');
            }
        }
    });

    // Auto-hide alerts
    setTimeout(function() {
        $('.alert-auto-hide').fadeOut(500);
    }, 5000);

    // Confirm delete actions
    $('[data-confirm-delete]').on('click', function(e) {
        if (!confirm('Bạn có chắc muốn xóa? Hành động này không thể hoàn tác.')) {
            e.preventDefault();
            return false;
        }
    });

    // Handle notification dropdown
    $('#notifications-btn').on('click', function(e) {
        e.stopPropagation();
        $('#notifications-dropdown').toggleClass('hidden');
    });

    // Close dropdown when clicking outside
    $(document).on('click', function(e) {
        if (!$(e.target).closest('#notifications-btn, #notifications-dropdown').length) {
            $('#notifications-dropdown').addClass('hidden');
        }
    });

    // Initialize select2 if available
    if ($.fn.select2) {
        $('select.select2').select2({
            theme: 'bootstrap4',
            width: '100%'
        });
    }

    // Auto-grow textarea
    $('textarea[data-auto-grow]').on('input', function() {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    });

    // Number input formatting
    $('input[data-format="currency"]').on('blur', function() {
        const value = parseFloat($(this).val());
        if (!isNaN(value)) {
            $(this).val(formatNumber(value));
        }
    }).on('focus', function() {
        const value = $(this).val().replace(/[^0-9.-]/g, '');
        $(this).val(value);
    });

    // Quick stats refresh
    $('.refresh-stats').on('click', function() {
        const $btn = $(this);
        $btn.addClass('fa-spin');

        $.ajax({
            url: '/Admin/Dashboard/Stats',
            method: 'GET',
            success: (response) => {
                if (response.success) {
                    // Update stats on page
                    $('#total-revenue').text(formatCurrency(response.data.totalRevenue));
                    $('#total-orders').text(formatNumber(response.data.totalOrders));
                    $('#total-books').text(formatNumber(response.data.totalBooks));
                    $('#total-users').text(formatNumber(response.data.totalUsers));
                    
                    showNotification('success', 'Đã cập nhật thống kê!', 2000);
                }
            },
            complete: () => {
                $btn.removeClass('fa-spin');
            }
        });
    });

    // Print functionality
    $('.btn-print').on('click', function() {
        window.print();
    });

    // Copy to clipboard
    $('.btn-copy').on('click', function() {
        const text = $(this).data('copy-text');
        navigator.clipboard.writeText(text).then(() => {
            showNotification('success', 'Đã sao chép!', 2000);
        });
    });
});