// ===== PRÉCHARGEUR =====
document.addEventListener('DOMContentLoaded', function() {
    // Matrice de texte
    const matrixText = document.getElementById('matrixText');
    const matrixChars = '01';
    const rows = 5;
    const cols = 60;
    
    function generateMatrix() {
        let html = '';
        for (let i = 0; i < rows; i++) {
            let row = '';
            for (let j = 0; j < cols; j++) {
                const char = matrixChars[Math.floor(Math.random() * matrixChars.length)];
                row += char;
            }
            html += `<div style="color: ${i === 2 ? '#00FF94' : '#00D9FF'}; opacity: ${0.3 + i * 0.2};">${row}</div>`;
        }
        matrixText.innerHTML = html;
    }
    
    setInterval(generateMatrix, 100);
    
    // Barre de progression
    const progressBar = document.querySelector('.progress-bar');
    const progressPercent = document.getElementById('progressPercent');
    let progress = 0;
    
    const progressInterval = setInterval(() => {
        progress += Math.random() * 5;
        if (progress > 100) {
            progress = 100;
            clearInterval(progressInterval);
            
            // Transition après chargement complet
            setTimeout(() => {
                document.getElementById('preloader').style.opacity = '0';
                setTimeout(() => {
                    document.getElementById('preloader').style.display = 'none';
                    document.body.style.cursor = 'default';
                    initAllEffects();
                }, 1000);
            }, 500);
        }
        
        progressBar.style.width = `${progress}%`;
        progressPercent.textContent = Math.floor(progress);
    }, 50);
    
    // Texte qui tape
    const messages = [
        'INITIALISATION DU SYSTÈME...',
        'CHARGEMENT DES MODULES...',
        'PRÉPARATION DE L\'EXPÉRIENCE...',
        'PRÊT À IMPRESSIONNER...'
    ];
    
    let currentMessage = 0;
    let currentChar = 0;
    const typingText = document.querySelector('.typing-text');
    
    function typeWriter() {
        if (currentChar < messages[currentMessage].length) {
            typingText.textContent += messages[currentMessage].charAt(currentChar);
            currentChar++;
            setTimeout(typeWriter, 50);
        } else {
            setTimeout(() => {
                typingText.textContent = '';
                currentChar = 0;
                currentMessage = (currentMessage + 1) % messages.length;
                typeWriter();
            }, 2000);
        }
    }
    
    typeWriter();
});

// ===== INITIALISATION DES EFFETS =====
function initAllEffects() {
    initHeroEffects();
    initMagneticButtons();
    initCountUp();
    initTiltEffect();
    initShowcase();
    initProcessTimeline();
    initPricingToggle();
    initContactForm();
    initCustomCursor();
    initScrollAnimations();
    initParticles();
}

// ===== HERO EFFECTS =====
function initHeroEffects() {
    // Titre qui tape
    const typingTitle = document.getElementById('typingTitle');
    const words = ['AMÉLIORENT', 'TRANSFORMENT', 'RÉVOLUTIONNENT', 'DOMINENT'];
    let wordIndex = 0;
    let charIndex = 0;
    let isDeleting = false;
    
    function type() {
        const currentWord = words[wordIndex];
        
        if (!isDeleting && charIndex <= currentWord.length) {
            typingTitle.textContent = currentWord.substring(0, charIndex);
            charIndex++;
            setTimeout(type, 100);
        } else if (isDeleting && charIndex >= 0) {
            typingTitle.textContent = currentWord.substring(0, charIndex);
            charIndex--;
            setTimeout(type, 50);
        } else {
            isDeleting = !isDeleting;
            if (!isDeleting) {
                wordIndex = (wordIndex + 1) % words.length;
            }
            setTimeout(type, 500);
        }
    }
    
    type();
    
    // Stats qui montent
    const stats = document.querySelectorAll('.hero-stats .stat-item');
    stats.forEach(stat => {
        const target = parseInt(stat.textContent);
        let current = 0;
        const increment = target / 50;
        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                current = target;
                clearInterval(timer);
            }
            stat.textContent = Math.floor(current);
        }, 30);
    });
}

// ===== BOUTONS MAGNÉTIQUES =====
function initMagneticButtons() {
    const magneticBtns = document.querySelectorAll('.magnetic-btn');
    
    magneticBtns.forEach(btn => {
        const strength = parseInt(btn.getAttribute('data-strength')) || 20;
        const radius = parseInt(btn.getAttribute('data-radius')) || 80;
        
        btn.addEventListener('mousemove', (e) => {
            const rect = btn.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;
            
            const distanceX = x - centerX;
            const distanceY = y - centerY;
            
            const distance = Math.sqrt(distanceX * distanceX + distanceY * distanceY);
            
            if (distance < radius) {
                const moveX = (distanceX / radius) * strength;
                const moveY = (distanceY / radius) * strength;
                
                btn.style.transform = `translate(${moveX}px, ${moveY}px)`;
            }
        });
        
        btn.addEventListener('mouseleave', () => {
            btn.style.transform = 'translate(0, 0)';
        });
    });
}

// ===== COUNT UP ANIMATION =====
function initCountUp() {
    const countElements = document.querySelectorAll('.count-up');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const element = entry.target;
                const target = parseInt(element.getAttribute('data-target'));
                const duration = 2000;
                const increment = target / (duration / 16);
                let current = 0;
                
                const timer = setInterval(() => {
                    current += increment;
                    if (current >= target) {
                        current = target;
                        clearInterval(timer);
                    }
                    element.textContent = Math.floor(current);
                    
                    // Ajouter des suffixes
                    if (target >= 1000000) {
                        element.textContent = Math.floor(current / 1000000) + 'M';
                    } else if (target >= 1000) {
                        element.textContent = Math.floor(current / 1000) + 'K';
                    } else {
                        element.textContent = Math.floor(current);
                    }
                }, 16);
                
                observer.unobserve(element);
            }
        });
    }, { threshold: 0.5 });
    
    countElements.forEach(element => observer.observe(element));
}

// ===== EFFET TILT 3D =====
function initTiltEffect() {
    const tiltElements = document.querySelectorAll('[data-tilt]');
    
    tiltElements.forEach(element => {
        element.addEventListener('mousemove', (e) => {
            const rect = element.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;
            
            const rotateY = ((x - centerX) / centerX) * 10;
            const rotateX = ((centerY - y) / centerY) * 10;
            
            element.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
        });
        
        element.addEventListener('mouseleave', () => {
            element.style.transform = 'perspective(1000px) rotateX(0) rotateY(0)';
        });
    });
}

// ===== SHOWCASE PORTFOLIO =====
function initShowcase() {
    const projects = [
        {
            title: 'NEXUS TECH',
            category: 'tech',
            stats: '+300% TRAFFIC',
            color: '#00D9FF'
        },
        {
            title: 'LUXURY ESTATE',
            category: 'luxe',
            stats: '€5M+ SALES',
            color: '#FF00E5'
        },
        {
            title: 'CORP SOLUTIONS',
            category: 'corporate',
            stats: '+500 LEADS/MONTH',
            color: '#00FF94'
        },
        {
            title: 'E-SHOP PRO',
            category: 'ecommerce',
            stats: '€2M+ REVENUE',
            color: '#FF6B00'
        },
        {
            title: 'FINTECH PRO',
            category: 'tech',
            stats: '50K+ USERS',
            color: '#9D00FF'
        },
        {
            title: 'PREMIUM BRAND',
            category: 'luxe',
            stats: '+200% ENGAGEMENT',
            color: '#00D9FF'
        }
    ];
    
    const showcaseGrid = document.getElementById('showcaseGrid');
    const categoryBtns = document.querySelectorAll('.category-btn');
    const prevBtn = document.querySelector('.prev-btn');
    const nextBtn = document.querySelector('.next-btn');
    
    let currentFilter = 'all';
    let currentIndex = 0;
    
    function renderProjects(filter = 'all') {
        showcaseGrid.innerHTML = '';
        
        const filteredProjects = filter === 'all' 
            ? projects 
            : projects.filter(p => p.category === filter);
        
        filteredProjects.forEach(project => {
            const projectElement = document.createElement('div');
            projectElement.className = 'showcase-item';
            projectElement.innerHTML = `
                <div class="showcase-image" style="background: linear-gradient(45deg, ${project.color}, ${project.color}80);">
                    <div class="project-overlay">
                        <div class="project-stats">${project.stats}</div>
                    </div>
                </div>
                <div class="showcase-content">
                    <h3 class="showcase-title">${project.title}</h3>
                    <span class="showcase-category">${project.category.toUpperCase()}</span>
                    <div class="project-hover">
                        <button class="view-project">VIEW PROJECT →</button>
                    </div>
                </div>
            `;
            
            showcaseGrid.appendChild(projectElement);
        });
    }
    
    // Filtres par catégorie
    categoryBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            categoryBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentFilter = btn.getAttribute('data-category');
            renderProjects(currentFilter);
        });
    });
    
    // Navigation
    prevBtn.addEventListener('click', () => {
        const items = showcaseGrid.querySelectorAll('.showcase-item');
        if (items.length > 0) {
            currentIndex = (currentIndex - 1 + items.length) % items.length;
            items[currentIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    });
    
    nextBtn.addEventListener('click', () => {
        const items = showcaseGrid.querySelectorAll('.showcase-item');
        if (items.length > 0) {
            currentIndex = (currentIndex + 1) % items.length;
            items[currentIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    });
    
    // Initial render
    renderProjects();
}

// ===== PROCESS TIMELINE =====
function initProcessTimeline() {
    const steps = document.querySelectorAll('.process-step');
    const timelineProgress = document.querySelector('.timeline-progress');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const step = entry.target;
                const stepNumber = parseInt(step.getAttribute('data-step'));
                const progressPercentage = (stepNumber / steps.length) * 100;
                
                timelineProgress.style.height = `${progressPercentage}%`;
                
                steps.forEach(s => s.classList.remove('active'));
                step.classList.add('active');
            }
        });
    }, { threshold: 0.5 });
    
    steps.forEach(step => observer.observe(step));
}

// ===== PRICING TOGGLE =====
function initPricingToggle() {
    const toggle = document.getElementById('pricingToggle');
    const amountElements = document.querySelectorAll('.amount');
    
    toggle.addEventListener('change', function() {
        const isMonthly = this.checked;
        
        amountElements.forEach(element => {
            const monthlyPrice = element.getAttribute('data-monthly');
            const projectPrice = element.getAttribute('data-project');
            
            element.textContent = isMonthly ? monthlyPrice : projectPrice;
            
            // Mettre à jour la période
            const periodElement = element.closest('.plan-price').nextElementSibling;
            if (periodElement && periodElement.classList.contains('plan-period')) {
                periodElement.textContent = isMonthly ? 'PAR MOIS' : 'PROJET COMPLET';
            }
        });
    });
}

// ===== CONTACT FORM =====
function initContactForm() {
    const form = document.getElementById('mainContactForm');
    const budgetOptions = document.querySelectorAll('.budget-option');
    let selectedBudget = null;
    
    // Sélection du budget
    budgetOptions.forEach(option => {
        option.addEventListener('click', () => {
            budgetOptions.forEach(opt => opt.classList.remove('active'));
            option.classList.add('active');
            selectedBudget = option.getAttribute('data-value');
        });
    });
    
    // Soumission du formulaire
    form.addEventListener('submit', function(e) {
        e.preventDefault();
        
        const submitBtn = form.querySelector('.submit-btn');
        const originalText = submitBtn.querySelector('.submit-text').textContent;
        const originalIcon = submitBtn.querySelector('.submit-icon').textContent;
        
        // Animation de soumission
        submitBtn.querySelector('.submit-text').textContent = 'TRANSMISSION...';
        submitBtn.querySelector('.submit-icon').textContent = '⏳';
        submitBtn.disabled = true;
        
        // Simulation d'envoi
        setTimeout(() => {
            // Confirmation visuelle
            submitBtn.querySelector('.submit-text').textContent = 'MESSAGE ENVOYÉ !';
            submitBtn.querySelector('.submit-icon').textContent = '✅';
            submitBtn.style.background = 'linear-gradient(45deg, #00FF94, #00D9FF)';
            
            // Effet de particules
            createParticles(submitBtn);
            
            // Réinitialisation après 2 secondes
            setTimeout(() => {
                submitBtn.querySelector('.submit-text').textContent = originalText;
                submitBtn.querySelector('.submit-icon').textContent = originalIcon;
                submitBtn.style.background = 'linear-gradient(45deg, var(--primary), var(--secondary))';
                submitBtn.disabled = false;
                
                form.reset();
                budgetOptions.forEach(opt => opt.classList.remove('active'));
                selectedBudget = null;
                
                // Notification
                showNotification('Votre message a été envoyé avec succès !');
            }, 2000);
        }, 1500);
    });
    
    function showNotification(message) {
        const notification = document.createElement('div');
        notification.className = 'notification';
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">✨</span>
                <span class="notification-text">${message}</span>
            </div>
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.classList.add('show');
        }, 10);
        
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }
    
    // Créer un style pour la notification
    const style = document.createElement('style');
    style.textContent = `
        .notification {
            position: fixed;
            top: 20px;
            right: 20px;
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 20px;
            transform: translateX(400px);
            transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            z-index: 10000;
            backdrop-filter: blur(10px);
            max-width: 400px;
        }
        
        .notification.show {
            transform: translateX(0);
        }
        
        .notification-content {
            display: flex;
            align-items: center;
            gap: 15px;
        }
        
        .notification-icon {
            font-size: 1.5rem;
            color: var(--accent);
        }
        
        .notification-text {
            color: var(--text-primary);
            font-size: 0.9rem;
        }
    `;
    document.head.appendChild(style);
}

// ===== CURSOR PERSONNALISÉ =====
function initCustomCursor() {
    const cursor = document.querySelector('.custom-cursor');
    const trail = document.querySelector('.cursor-trail');
    let mouseX = 0;
    let mouseY = 0;
    let trailX = 0;
    let trailY = 0;
    
    document.addEventListener('mousemove', (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
    });
    
    function animateCursor() {
        // Cursor principal
        cursor.style.left = `${mouseX - 10}px`;
        cursor.style.top = `${mouseY - 10}px`;
        
        // Trail avec effet de retard
        trailX += (mouseX - trailX - 4) * 0.1;
        trailY += (mouseY - trailY - 4) * 0.1;
        
        trail.style.left = `${trailX}px`;
        trail.style.top = `${trailY}px`;
        
        // Effet d'agrandissement sur les éléments interactifs
        const hoverElements = document.querySelectorAll('a, button, .feature-card, .showcase-item, .pricing-card');
        let isHovering = false;
        
        hoverElements.forEach(element => {
            const rect = element.getBoundingClientRect();
            if (
                mouseX >= rect.left &&
                mouseX <= rect.right &&
                mouseY >= rect.top &&
                mouseY <= rect.bottom
            ) {
                isHovering = true;
            }
        });
        
        if (isHovering) {
            cursor.style.transform = 'scale(1.5)';
            cursor.style.borderColor = '#FF00E5';
            trail.style.transform = 'scale(1.5)';
            trail.style.background = '#FF00E5';
        } else {
            cursor.style.transform = 'scale(1)';
            cursor.style.borderColor = '#00D9FF';
            trail.style.transform = 'scale(1)';
            trail.style.background = '#00D9FF';
        }
        
        requestAnimationFrame(animateCursor);
    }
    
    animateCursor();
}

// ===== ANIMATIONS AU SCROLL =====
function initScrollAnimations() {
    const animatedElements = document.querySelectorAll('.feature-card, .showcase-item, .process-step, .pricing-card');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
            }
        });
    }, { threshold: 0.1 });
    
    animatedElements.forEach(element => observer.observe(element));
    
    // Ajouter un style pour les animations
    const style = document.createElement('style');
    style.textContent = `
        .feature-card,
        .showcase-item,
        .process-step,
        .pricing-card {
            opacity: 0;
            transform: translateY(30px);
            transition: opacity 0.6s ease, transform 0.6s ease;
        }
        
        .feature-card.animate-in,
        .showcase-item.animate-in,
        .process-step.animate-in,
        .pricing-card.animate-in {
            opacity: 1;
            transform: translateY(0);
        }
    `;
    document.head.appendChild(style);
}

// ===== PARTICULES =====
function initParticles() {
    const particlesContainer = document.getElementById('heroParticles');
    const particleCount = 50;
    
    for (let i = 0; i < particleCount; i++) {
        createParticle(particlesContainer);
    }
}

function createParticle(container) {
    const particle = document.createElement('div');
    particle.className = 'particle';
    
    // Position aléatoire
    const x = Math.random() * 100;
    const y = Math.random() * 100;
    
    // Taille aléatoire
    const size = Math.random() * 4 + 1;
    
    // Couleur aléatoire
    const colors = ['#00D9FF', '#FF00E5', '#00FF94'];
    const color = colors[Math.floor(Math.random() * colors.length)];
    
    // Animation aléatoire
    const duration = Math.random() * 10 + 10;
    const delay = Math.random() * 5;
    
    particle.style.cssText = `
        position: absolute;
        top: ${y}%;
        left: ${x}%;
        width: ${size}px;
        height: ${size}px;
        background: ${color};
        border-radius: 50%;
        pointer-events: none;
        opacity: ${Math.random() * 0.5 + 0.2};
        animation: float ${duration}s linear ${delay}s infinite;
    `;
    
    container.appendChild(particle);
}

function createParticles(button) {
    const rect = button.getBoundingClientRect();
    const particleCount = 20;
    
    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.className = 'btn-particle';
        
        const colors = ['#00D9FF', '#FF00E5', '#00FF94', '#FFFFFF'];
        const color = colors[Math.floor(Math.random() * colors.length)];
        
        const size = Math.random() * 6 + 2;
        const angle = Math.random() * Math.PI * 2;
        const distance = Math.random() * 50 + 30;
        
        particle.style.cssText = `
            position: absolute;
            left: 50%;
            top: 50%;
            width: ${size}px;
            height: ${size}px;
            background: ${color};
            border-radius: 50%;
            pointer-events: none;
            transform: translate(-50%, -50%) rotate(${angle}rad) translateX(${distance}px);
            opacity: 1;
            animation: particleExplode 0.6s ease-out forwards;
        `;
        
        button.querySelector('.btn-particles').appendChild(particle);
        
        setTimeout(() => {
            particle.remove();
        }, 600);
    }
}

// Ajouter les animations CSS pour les particules
const particleStyles = document.createElement('style');
particleStyles.textContent = `
    @keyframes float {
        0% {
            transform: translate(0, 0) rotate(0deg);
            opacity: 0;
        }
        10% {
            opacity: 1;
        }
        90% {
            opacity: 1;
        }
        100% {
            transform: translate(${Math.random() * 100 - 50}px, ${Math.random() * 100 - 50}px) rotate(${Math.random() * 360}deg);
            opacity: 0;
        }
    }
    
    @keyframes particleExplode {
        0% {
            transform: translate(-50%, -50%) scale(1);
            opacity: 1;
        }
        100% {
            transform: translate(-50%, -50%) translateX(var(--tx, 0)) translateY(var(--ty, 0)) scale(0);
            opacity: 0;
        }
    }
    
    .btn-particle {
        --tx: ${Math.random() * 100 - 50}px;
        --ty: ${Math.random() * 100 - 50}px;
    }
`;
document.head.appendChild(particleStyles);

// ===== NAVIGATION RESPONSIVE =====
const hamburger = document.querySelector('.nav-hamburger');
const navLinks = document.querySelector('.nav-links');

hamburger.addEventListener('click', () => {
    navLinks.style.display = navLinks.style.display === 'flex' ? 'none' : 'flex';
});

// ===== OBSERVER POUR LA NAVIGATION =====
const sections = document.querySelectorAll('section[id]');
const navItems = document.querySelectorAll('.nav-link');

window.addEventListener('scroll', () => {
    let current = '';
    
    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionHeight = section.clientHeight;
        
        if (scrollY >= sectionTop - 200) {
            current = section.getAttribute('id');
        }
    });
    
    navItems.forEach(item => {
        item.classList.remove('active');
        if (item.getAttribute('href') === `#${current}`) {
            item.classList.add('active');
        }
    });
});

// ===== INITIALISATION =====
window.addEventListener('load', initAllEffects);