function togglePassword(button) {
    const input = button.parentElement.querySelector('input');
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.className = icon.getAttribute('data-hide');
    } else {
        input.type = 'password';
        icon.className = icon.getAttribute('data-show');
    }
}

// Form validation
document.addEventListener('DOMContentLoaded', function() {
    const form = document.querySelector('form');
    const submitBtn = form.querySelector('button[type="submit"]');
    
    // Real-time validation
    const inputs = form.querySelectorAll('input[required]');
    inputs.forEach(input => {
        input.addEventListener('blur', function() {
            validateField(this);
        });
        
        input.addEventListener('input', function() {
            if (this.classList.contains('border-red-500')) {
                validateField(this);
            }
        });
    });

    // Form submission
    form.addEventListener('submit', function(e) {
        let isValid = true;
        
        inputs.forEach(input => {
            if (!validateField(input)) {
                isValid = false;
            }
        });

        if (!isValid) {
            e.preventDefault();
            return false;
        }

        // Show loading state
        submitBtn.disabled = true;
        submitBtn.innerHTML = `
            <span class="flex items-center justify-center space-x-2">
                <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                <span>Đang xử lý...</span>
            </span>
        `;

        // Show loading spinner
        document.getElementById('loading-spinner').classList.remove('hidden');
    });

    function validateField(field) {
        const value = field.value.trim();
        let isValid = true;
        let errorMessage = '';

        // Reset field state
        field.classList.remove('border-red-500', 'border-green-500');
        const errorSpan = field.parentElement.querySelector('.text-red-500');
        if (errorSpan) {
            errorSpan.textContent = '';
        }

        // Required validation
        if (field.hasAttribute('required') && !value) {
            isValid = false;
            errorMessage = 'Trường này là bắt buộc';
        }

        // Email validation
        if (field.type === 'email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const phoneRegex = /^[0-9]{10,11}$/;
            
            if (!emailRegex.test(value) && !phoneRegex.test(value)) {
                isValid = false;
                errorMessage = 'Email hoặc số điện thoại không hợp lệ';
            }
        }

        // Password validation
        if (field.type === 'password' && value) {
            if (value.length < 6) {
                isValid = false;
                errorMessage = 'Mật khẩu phải có ít nhất 6 ký tự';
            }
        }

        // Apply validation styles
        if (!isValid) {
            field.classList.add('border-red-500');
            if (errorSpan) {
                errorSpan.textContent = errorMessage;
            }
        } else if (value) {
            field.classList.add('border-green-500');
        }

        return isValid;
    }

    // Auto-hide error messages after successful correction
    const validationSummary = document.querySelector('[data-validation-summary]');
    if (validationSummary && validationSummary.textContent.trim()) {
        setTimeout(() => {
            validationSummary.style.display = 'none';
        }, 5000);
    }

    // Show success message if redirected back with success
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('success') === 'true') {
        if (window.BV) {
            window.BV.toast('Đăng ký thành công! Vui lòng đăng nhập.', 'success');
        }
    }
});

// Auto-focus first input
window.addEventListener('load', function() {
    const firstInput = document.querySelector('.auth-input');
    if (firstInput) {
        firstInput.focus();
    }
});

function togglePassword(button) {
    const input = button.parentElement.querySelector('input');
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.className = icon.getAttribute('data-hide');
    } else {
        input.type = 'password';
        icon.className = icon.getAttribute('data-show');
    }
}

// Password strength checker
function checkPasswordStrength(password) {
    const bars = document.querySelectorAll('.password-strength-bar');
    const text = document.querySelector('.password-strength-text');
    
    let strength = 0;
    let message = '';
    
    if (password.length >= 6) strength++;
    if (password.length >= 8) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;

    // Reset bars
    bars.forEach(bar => {
        bar.className = 'password-strength-bar bg-gray-200 h-1 rounded-full flex-1';
    });

    // Set strength colors and message
    if (strength >= 1) {
        bars[0].classList.add('bg-red-500');
        message = 'Mật khẩu yếu';
    }
    if (strength >= 2) {
        bars[1].classList.add('bg-orange-500');
        message = 'Mật khẩu trung bình';
    }
    if (strength >= 3) {
        bars[2].classList.add('bg-yellow-500');
        message = 'Mật khẩu khá mạnh';
    }
    if (strength >= 4) {
        bars[3].classList.add('bg-green-500');
        message = 'Mật khẩu mạnh';
    }

    text.textContent = password.length > 0 ? message : 'Mật khẩu nên có ít nhất 6 ký tự';
    text.className = `password-strength-text text-xs mt-1 ${
        strength >= 3 ? 'text-green-600' : 
        strength >= 2 ? 'text-yellow-600' : 
        strength >= 1 ? 'text-orange-600' : 'text-gray-500'
    }`;
}

// Form validation and enhancement
document.addEventListener('DOMContentLoaded', function() {
    const form = document.querySelector('form');
    const submitBtn = form.querySelector('button[type="submit"]');
    const passwordInput = document.querySelector('input[name="Password"]');
    const confirmPasswordInput = document.querySelector('input[name="ConfirmPassword"]');

    // Password strength monitoring
    passwordInput.addEventListener('input', function() {
        checkPasswordStrength(this.value);
        validateField(this);
    });

    // Confirm password validation
    confirmPasswordInput.addEventListener('input', function() {
        validatePasswordMatch();
    });

    // Real-time validation for all fields
    const inputs = form.querySelectorAll('input[required]');
    inputs.forEach(input => {
        input.addEventListener('blur', function() {
            validateField(this);
        });
        
        input.addEventListener('input', function() {
            if (this.classList.contains('border-red-500')) {
                validateField(this);
            }
        });
    });

    // Form submission
    form.addEventListener('submit', function(e) {
        let isValid = true;
        
        inputs.forEach(input => {
            if (!validateField(input)) {
                isValid = false;
            }
        });

        if (!validatePasswordMatch()) {
            isValid = false;
        }

        const agreeTerms = document.querySelector('input[name="AgreeToTerms"]');
        if (!agreeTerms.checked) {
            isValid = false;
            showFieldError(agreeTerms, 'Vui lòng đồng ý với điều khoản sử dụng');
        }

        if (!isValid) {
            e.preventDefault();
            return false;
        }

        // Show loading state
        submitBtn.disabled = true;
        submitBtn.innerHTML = `
            <span class="flex items-center justify-center space-x-2">
                <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                <span>Đang tạo tài khoản...</span>
            </span>
        `;

        document.getElementById('loading-spinner').classList.remove('hidden');
    });

    function validateField(field) {
        const value = field.value.trim();
        let isValid = true;
        let errorMessage = '';

        // Reset field state
        field.classList.remove('border-red-500', 'border-green-500');
        hideFieldError(field);

        // Required validation
        if (field.hasAttribute('required') && !value) {
            isValid = false;
            errorMessage = 'Trường này là bắt buộc';
        }

        // Specific field validations
        if (field.name === 'Email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                isValid = false;
                errorMessage = 'Email không hợp lệ';
            }
        }

        if (field.name === 'PhoneNumber' && value) {
            const phoneRegex = /^[0-9]{10,11}$/;
            if (!phoneRegex.test(value)) {
                isValid = false;
                errorMessage = 'Số điện thoại không hợp lệ';
            }
        }

        if (field.name === 'Password' && value) {
            if (value.length < 6) {
                isValid = false;
                errorMessage = 'Mật khẩu phải có ít nhất 6 ký tự';
            }
        }

        if (field.name === 'FullName' && value) {
            if (value.length < 2) {
                isValid = false;
                errorMessage = 'Họ tên phải có ít nhất 2 ký tự';
            }
        }

        // Apply validation styles
        if (!isValid) {
            field.classList.add('border-red-500');
            showFieldError(field, errorMessage);
        } else if (value) {
            field.classList.add('border-green-500');
        }

        return isValid;
    }

    function validatePasswordMatch() {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;
        
        if (confirmPassword && password !== confirmPassword) {
            confirmPasswordInput.classList.add('border-red-500');
            showFieldError(confirmPasswordInput, 'Mật khẩu xác nhận không khớp');
            return false;
        } else if (confirmPassword) {
            confirmPasswordInput.classList.remove('border-red-500');
            confirmPasswordInput.classList.add('border-green-500');
            hideFieldError(confirmPasswordInput);
        }
        
        return true;
    }

    function showFieldError(field, message) {
        const errorSpan = field.parentElement.querySelector('.text-red-500');
        if (errorSpan) {
            errorSpan.textContent = message;
        }
    }

    function hideFieldError(field) {
        const errorSpan = field.parentElement.querySelector('.text-red-500');
        if (errorSpan) {
            errorSpan.textContent = '';
        }
    }
});

// Auto-focus first input
window.addEventListener('load', function() {
    const firstInput = document.querySelector('.auth-input');
    if (firstInput) {
        firstInput.focus();
    }
});

// wwwroot/js/auth-form.js
/**
 * Authentication Forms Enhancement
 * Handles login and registration form interactions
 */

class AuthFormManager {
    constructor() {
        this.init();
    }

    init() {
        this.initFloatingLabels();
        this.initPasswordStrength();
        this.initFormValidation();
        this.initSocialAuth();
        this.initAccessibility();
    }

    /**
     * Floating Labels Animation
     */
    initFloatingLabels() {
        const inputs = document.querySelectorAll('.auth-input');
        
        inputs.forEach(input => {
            const label = input.previousElementSibling;
            
            if (label && label.tagName === 'LABEL') {
                // Handle focus/blur events
                input.addEventListener('focus', () => {
                    this.activateLabel(label);
                });
                
                input.addEventListener('blur', () => {
                    if (!input.value.trim()) {
                        this.deactivateLabel(label);
                    }
                });

                // Check initial state
                if (input.value.trim()) {
                    this.activateLabel(label);
                }

                // Handle autofill detection
                this.detectAutofill(input, label);
            }
        });
    }

    activateLabel(label) {
        label.classList.add('text-primary-600', 'transform', '-translate-y-6', 'scale-75');
        label.style.zIndex = '10';
    }

    deactivateLabel(label) {
        label.classList.remove('text-primary-600', 'transform', '-translate-y-6', 'scale-75');
        label.style.zIndex = '1';
    }

    detectAutofill(input, label) {
        // Check for autofill periodically
        const checkAutofill = () => {
            if (input.matches(':-webkit-autofill') || input.value) {
                this.activateLabel(label);
            }
        };

        setTimeout(checkAutofill, 100);
        setTimeout(checkAutofill, 500);
        setTimeout(checkAutofill, 1000);
    }

    /**
     * Password Strength Indicator
     */
    initPasswordStrength() {
        const passwordInputs = document.querySelectorAll('input[type="password"][name="Password"]');
        
        passwordInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                this.updatePasswordStrength(e.target.value);
            });
        });
    }

    updatePasswordStrength(password) {
        const bars = document.querySelectorAll('.password-strength-bar');
        const text = document.querySelector('.password-strength-text');
        
        if (!bars.length) return;

        let strength = 0;
        let message = 'Mật khẩu nên có ít nhất 6 ký tự';
        let colorClass = 'text-gray-500';

        // Calculate strength
        if (password.length >= 6) strength++;
        if (password.length >= 8) strength++;
        if (/[A-Z]/.test(password)) strength++;
        if (/[a-z]/.test(password)) strength++;
        if (/[0-9]/.test(password)) strength++;
        if (/[^A-Za-z0-9]/.test(password)) strength++;

        // Reset bars
        bars.forEach(bar => {
            bar.className = 'password-strength-bar bg-gray-200 h-1 rounded-full flex-1 transition-all duration-300';
        });

        // Apply strength visualization
        if (password.length > 0) {
            if (strength >= 1) {
                bars[0].classList.add('bg-red-500');
                message = 'Mật khẩu yếu';
                colorClass = 'text-red-600';
            }
            if (strength >= 2) {
                bars[1].classList.add('bg-orange-500');
                message = 'Mật khẩu trung bình';
                colorClass = 'text-orange-600';
            }
            if (strength >= 4) {
                bars[2].classList.add('bg-yellow-500');
                message = 'Mật khẩu khá mạnh';
                colorClass = 'text-yellow-600';
            }
            if (strength >= 5) {
                bars[3].classList.add('bg-green-500');
                message = 'Mật khẩu mạnh';
                colorClass = 'text-green-600';
            }
        }

        if (text) {
            text.textContent = message;
            text.className = `password-strength-text text-xs mt-1 transition-colors duration-300 ${colorClass}`;
        }
    }

    /**
     * Form Validation
     */
    initFormValidation() {
        const forms = document.querySelectorAll('form[asp-action="Login"], form[asp-action="Register"]');
        
        forms.forEach(form => {
            const inputs = form.querySelectorAll('input[required]');
            const submitBtn = form.querySelector('button[type="submit"]');

            // Real-time validation
            inputs.forEach(input => {
                input.addEventListener('blur', () => this.validateField(input));
                input.addEventListener('input', () => {
                    if (input.classList.contains('border-red-500')) {
                        this.validateField(input);
                    }
                });
            });

            // Form submission
            form.addEventListener('submit', (e) => {
                if (!this.validateForm(form)) {
                    e.preventDefault();
                } else {
                    this.showLoadingState(submitBtn);
                }
            });
        });
    }

    validateField(field) {
        const value = field.value.trim();
        let isValid = true;
        let errorMessage = '';

        // Reset field state
        field.classList.remove('border-red-500', 'border-green-500');
        this.hideFieldError(field);

        // Required validation
        if (field.hasAttribute('required') && !value) {
            isValid = false;
            errorMessage = this.getRequiredMessage(field);
        }

        // Type-specific validation
        if (value && isValid) {
            switch (field.type) {
                case 'email':
                    if (!this.validateEmail(value)) {
                        isValid = false;
                        errorMessage = 'Email không hợp lệ';
                    }
                    break;
                case 'tel':
                    if (!this.validatePhone(value)) {
                        isValid = false;
                        errorMessage = 'Số điện thoại không hợp lệ';
                    }
                    break;
                case 'password':
                    if (field.name === 'Password' && value.length < 6) {
                        isValid = false;
                        errorMessage = 'Mật khẩu phải có ít nhất 6 ký tự';
                    } else if (field.name === 'ConfirmPassword') {
                        const passwordField = document.querySelector('input[name="Password"]');
                        if (passwordField && value !== passwordField.value) {
                            isValid = false;
                            errorMessage = 'Mật khẩu xác nhận không khớp';
                        }
                    }
                    break;
                case 'text':
                    if (field.name === 'FullName' && value.length < 2) {
                        isValid = false;
                        errorMessage = 'Họ tên phải có ít nhất 2 ký tự';
                    }
                    break;
            }
        }

        // Apply validation styles
        if (!isValid) {
            field.classList.add('border-red-500');
            this.showFieldError(field, errorMessage);
        } else if (value) {
            field.classList.add('border-green-500');
        }

        return isValid;
    }

    validateForm(form) {
        let isValid = true;
        const inputs = form.querySelectorAll('input[required]');
        
        inputs.forEach(input => {
            if (!this.validateField(input)) {
                isValid = false;
            }
        });

        // Check terms agreement for registration
        const agreeTerms = form.querySelector('input[name="AgreeToTerms"]');
        if (agreeTerms && !agreeTerms.checked) {
            isValid = false;
            this.showFieldError(agreeTerms, 'Vui lòng đồng ý với điều khoản sử dụng');
        }

        return isValid;
    }

    validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const phoneRegex = /^[0-9]{10,11}$/;
        return emailRegex.test(email) || phoneRegex.test(email);
    }

    validatePhone(phone) {
        const phoneRegex = /^[0-9]{10,11}$/;
        return phoneRegex.test(phone);
    }

    getRequiredMessage(field) {
        const messages = {
            'Email': 'Vui lòng nhập email',
            'Password': 'Vui lòng nhập mật khẩu',
            'ConfirmPassword': 'Vui lòng xác nhận mật khẩu',
            'FullName': 'Vui lòng nhập họ tên',
            'PhoneNumber': 'Vui lòng nhập số điện thoại'
        };
        return messages[field.name] || 'Trường này là bắt buộc';
    }

    showFieldError(field, message) {
        const errorSpan = field.parentElement.querySelector('.text-red-500');
        if (errorSpan) {
            errorSpan.textContent = message;
            errorSpan.style.display = 'block';
        }
    }

    hideFieldError(field) {
        const errorSpan = field.parentElement.querySelector('.text-red-500');
        if (errorSpan) {
            errorSpan.textContent = '';
            errorSpan.style.display = 'none';
        }
    }

    showLoadingState(button) {
        const originalContent = button.innerHTML;
        button.disabled = true;
        button.innerHTML = `
            <span class="flex items-center justify-center space-x-2">
                <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                <span>Đang xử lý...</span>
            </span>
        `;

        // Show loading spinner overlay
        const spinner = document.getElementById('loading-spinner');
        if (spinner) {
            spinner.classList.remove('hidden');
        }

        // Reset button after 30 seconds (fallback)
        setTimeout(() => {
            button.disabled = false;
            button.innerHTML = originalContent;
            if (spinner) {
                spinner.classList.add('hidden');
            }
        }, 30000);
    }

    /**
     * Social Authentication
     */
    initSocialAuth() {
        const socialButtons = document.querySelectorAll('button[type="button"]');
        
        socialButtons.forEach(button => {
            if (button.textContent.includes('Google') || button.textContent.includes('Facebook')) {
                button.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.handleSocialAuth(button);
                });
            }
        });
    }

    handleSocialAuth(button) {
        const provider = button.textContent.includes('Google') ? 'Google' : 'Facebook';
        
        // Show loading state
        const originalContent = button.innerHTML;
        button.disabled = true;
        button.innerHTML = `
            <span class="flex items-center justify-center space-x-2">
                <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-400"></div>
                <span>Đang kết nối...</span>
            </span>
        `;

        // Simulate social auth (replace with actual implementation)
        setTimeout(() => {
            button.disabled = false;
            button.innerHTML = originalContent;
            
            if (window.BV) {
                window.BV.toast(`Đăng nhập với ${provider} sẽ sớm được hỗ trợ`, 'info');
            }
        }, 2000);
    }

    /**
     * Accessibility Enhancements
     */
    initAccessibility() {
        // Add ARIA labels
        const inputs = document.querySelectorAll('.auth-input');
        inputs.forEach(input => {
            const label = input.previousElementSibling;
            if (label) {
                const labelId = `label-${Math.random().toString(36).substr(2, 9)}`;
                label.id = labelId;
                input.setAttribute('aria-labelledby', labelId);
            }
        });

        // Handle keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                document.body.classList.add('user-is-tabbing');
            }
        });

        document.addEventListener('mousedown', () => {
            document.body.classList.remove('user-is-tabbing');
        });

        // Announce errors to screen readers
        const errorElements = document.querySelectorAll('.text-red-500');
        errorElements.forEach(element => {
            element.setAttribute('aria-live', 'polite');
            element.setAttribute('aria-atomic', 'true');
        });
    }
}

// Password visibility toggle function (global)
window.togglePassword = function(button) {
    const input = button.parentElement.querySelector('input');
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.className = icon.getAttribute('data-hide');
        button.setAttribute('aria-label', 'Ẩn mật khẩu');
    } else {
        input.type = 'password';
        icon.className = icon.getAttribute('data-show');
        button.setAttribute('aria-label', 'Hiện mật khẩu');
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AuthFormManager();
    
    // Auto-focus first input
    const firstInput = document.querySelector('.auth-input');
    if (firstInput) {
        setTimeout(() => firstInput.focus(), 100);
    }
    
    // Handle back navigation
    window.addEventListener('pageshow', (e) => {
        if (e.persisted) {
            // Reset form if coming back from cache
            const forms = document.querySelectorAll('form');
            forms.forEach(form => {
                const submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) {
                    submitBtn.disabled = false;
                }
            });
            
            const spinner = document.getElementById('loading-spinner');
            if (spinner) {
                spinner.classList.add('hidden');
            }
        }
    });
});

// Additional CSS for auth forms (to be added to site.css)

