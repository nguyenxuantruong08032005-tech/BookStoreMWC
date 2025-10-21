// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Mobile menu functionality
document.addEventListener('DOMContentLoaded', () => {
    const mobileMenuBtn = document.getElementById('mobile-menu-btn');
    const mobileMenu = document.getElementById('mobile-menu');
    const mobileOverlay = document.getElementById('mobile-overlay');
    const closeMobileMenu = document.getElementById('close-mobile-menu');
  
    function openMenu() {
      mobileMenu.classList.add('active');
      mobileOverlay.classList.add('active');
      document.body.style.overflow = 'hidden';
    }
  
    function closeMenu() {
      mobileMenu.classList.remove('active');
      mobileOverlay.classList.remove('active');
      document.body.style.overflow = '';
    }
  
    mobileMenuBtn.addEventListener('click', openMenu);
    closeMobileMenu.addEventListener('click', closeMenu);
    mobileOverlay.addEventListener('click', closeMenu);
  });
  

// Search functionality
document.querySelector('input[type="text"]').addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        const searchTerm = this.value;
        if (searchTerm.trim()) {
            console.log('Searching for:', searchTerm);
            // Here you would implement actual search functionality
        }
    }
});

// Search button click
// document.querySelector('.bg-blue-500.hover\\:bg-blue-600').addEventListener('click', function () {
//     const searchInput = document.querySelector('input[type="text"]');
//     const searchTerm = searchInput.value;
//     if (searchTerm.trim()) {
//         console.log('Searching for:', searchTerm);
//         // Here you would implement actual search functionality
//     }
// });

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth'
            });
        }
    });
});


document.addEventListener('DOMContentLoaded', function () {
    // Password toggle functionality
    const togglePassword = document.getElementById('togglePassword');
    // const passwordInput = document.getElementById('password');
    const eyeIcon = document.getElementById('eyeIcon');

    if (togglePassword && passwordInput && eyeIcon) {
        togglePassword.addEventListener('click', function () {
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);

            if (type === 'password') {
                eyeIcon.classList.remove('fa-eye-slash');
                eyeIcon.classList.add('fa-eye');
            } else {
                eyeIcon.classList.remove('fa-eye');
                eyeIcon.classList.add('fa-eye-slash');
            }
        });
    }

    // Form validation
    const form = document.getElementById('loginForm');
    const emailInput = document.querySelector('input[name="Email"]');
    const passwordInput = document.querySelector('input[name="Password"]');
    const submitBtn = document.getElementById('submitBtn');
    const submitText = document.getElementById('submitText');

    // Real-time validation
    function validateEmail() {
        const email = emailInput.value.trim();
        const emailError = emailInput.parentNode.parentNode.querySelector('.text-red-500');

        if (!email) {
            showFieldError(emailInput, emailError, 'Email là bắt buộc');
            return false;
        } else if (!isValidEmail(email)) {
            showFieldError(emailInput, emailError, 'Email không hợp lệ');
            return false;
        } else {
            hideFieldError(emailInput, emailError);
            return true;
        }
    }

    function validatePassword() {
        const password = passwordInput.value;
        const passwordError = passwordInput.parentNode.parentNode.querySelector('.text-red-500');

        if (!password) {
            showFieldError(passwordInput, passwordError, 'Mật khẩu là bắt buộc');
            return false;
        } else if (password.length < 6) {
            showFieldError(passwordInput, passwordError, 'Mật khẩu phải có ít nhất 6 ký tự');
            return false;
        } else {
            hideFieldError(passwordInput, passwordError);
            return true;
        }
    }

    function showFieldError(input, errorElement, message) {
        input.classList.add('border-red-500', 'focus:border-red-500', 'focus:ring-red-500');
        input.classList.remove('border-gray-300', 'focus:border-primary-500', 'focus:ring-primary-500');
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.classList.remove('hidden');
        }
    }

    function hideFieldError(input, errorElement) {
        input.classList.remove('border-red-500', 'focus:border-red-500', 'focus:ring-red-500');
        input.classList.add('border-gray-300', 'focus:border-primary-500', 'focus:ring-primary-500');
        if (errorElement) {
            errorElement.classList.add('hidden');
        }
    }

    function isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Add event listeners
    emailInput.addEventListener('blur', validateEmail);
    passwordInput.addEventListener('blur', validatePassword);

    // Form submission
    form.addEventListener('submit', function (e) {
        e.preventDefault();

        const isEmailValid = validateEmail();
        const isPasswordValid = validatePassword();

        if (isEmailValid && isPasswordValid) {
            // Show loading state
            submitBtn.disabled = true;
            submitText.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Đang đăng nhập...';

            // Submit form
            setTimeout(() => {
                form.submit();
            }, 500);
        } else {
            // Show error message
            BV.toast('Vui lòng kiểm tra lại thông tin đăng nhập', 'error');
        }
    });

    // Social login handlers
    const socialButtons = document.querySelectorAll('button[type="button"]');
    socialButtons.forEach(button => {
        button.addEventListener('click', function () {
            const provider = this.textContent.includes('Google') ? 'Google' : 'Facebook';
            BV.toast(`Đang chuyển hướng đến ${provider}...`, 'info');

            // In real implementation, redirect to OAuth provider
            setTimeout(() => {
                if (provider === 'Google') {
                    window.location.href = '/Account/ExternalLogin?provider=Google';
                } else {
                    window.location.href = '/Account/ExternalLogin?provider=Facebook';
                }
            }, 1000);
        });
    });

    // Auto-fill demo (for development)
    if (window.location.hostname === 'localhost' && document.querySelector('.demo-auto-fill')) {
        setTimeout(() => {
            emailInput.value = 'demo@bookverse.vn';
            passwordInput.value = 'Demo123!';
        }, 1000);
    }
});


 // Newsletter subscription
 document.addEventListener('DOMContentLoaded', function() {
    const newsletterForm = document.querySelector('form[asp-controller="Newsletter"]');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const email = this.querySelector('input[name="email"]').value;
            const submitBtn = this.querySelector('button[type="submit"]');
            
            if (!email || !validateEmail(email)) {
                if (window.BV) {
                    window.BV.toast('Vui lòng nhập email hợp lệ', 'error');
                }
                return;
            }
            
            // Show loading state
            const originalContent = submitBtn.innerHTML;
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Đang xử lý...';
            
            // Simulate API call (replace with actual implementation)
            setTimeout(() => {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalContent;
                
                if (window.BV) {
                    window.BV.toast('Đăng ký thành công! Cảm ơn bạn đã quan tâm.', 'success');
                }
                
                // Clear form
                this.querySelector('input[name="email"]').value = '';
            }, 2000);
        });
    }
    
    function validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Cookie consent
    const cookieBanner = document.getElementById('cookie-banner');
    const cookieAccept = document.getElementById('cookie-accept');
    const cookieDecline = document.getElementById('cookie-decline');

    // Check if user has already made a choice
    if (!localStorage.getItem('cookieConsent')) {
        setTimeout(() => {
            cookieBanner.classList.remove('translate-y-full');
        }, 3000);
    }

    cookieAccept.addEventListener('click', function() {
        localStorage.setItem('cookieConsent', 'accepted');
        cookieBanner.classList.add('translate-y-full');
        
        if (window.BV) {
            window.BV.toast('Cảm ơn bạn đã chấp nhận sử dụng cookie', 'success');
        }
    });

    cookieDecline.addEventListener('click', function() {
        localStorage.setItem('cookieConsent', 'declined');
        cookieBanner.classList.add('translate-y-full');
        
        if (window.BV) {
            window.BV.toast('Đã từ chối cookie. Một số tính năng có thể bị hạn chế.', 'info');
        }
    });

    // Smooth scroll for footer links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Social media click tracking (placeholder)
    document.querySelectorAll('.fab').forEach(icon => {
        icon.closest('a').addEventListener('click', function(e) {
            e.preventDefault();
            const platform = this.querySelector('i').className.includes('facebook') ? 'Facebook' :
                           this.querySelector('i').className.includes('instagram') ? 'Instagram' :
                           this.querySelector('i').className.includes('twitter') ? 'Twitter' :
                           this.querySelector('i').className.includes('youtube') ? 'YouTube' :
                           this.querySelector('i').className.includes('linkedin') ? 'LinkedIn' : 'Unknown';
            
            console.log(`Social media click: ${platform}`);
            
            if (window.BV) {
                window.BV.toast(`Đang chuyển đến ${platform}...`, 'info', 2000);
            }
            
            // In real implementation, open the actual social media links
            // window.open(this.href, '_blank');
        });
    });
});