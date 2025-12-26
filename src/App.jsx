// src/App.jsx
import React, { useEffect, useState, useRef } from 'react'
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'

/* ---------------- Canvas Background Spectaculaire ---------------- */
function StarsCanvas() {
  const ref = useRef(null)
  
  useEffect(() => {
    const canvas = ref.current
    if (!canvas) return
    
    const ctx = canvas.getContext('2d')
    let w = canvas.width = window.innerWidth
    let h = canvas.height = window.innerHeight
    
    const stars = Array.from({ length: 300 }).map(() => ({
      x: Math.random() * w,
      y: Math.random() * h,
      r: Math.random() * 2 + 0.5,
      a: Math.random() * 0.8 + 0.2,
      speed: Math.random() * 0.8 + 0.2,
      glow: Math.random() * 10 + 5
    }))
    
    const goldenParticles = Array.from({ length: 50 }).map(() => ({
      x: Math.random() * w,
      y: Math.random() * h,
      size: Math.random() * 3 + 1,
      speedX: (Math.random() - 0.5) * 0.5,
      speedY: (Math.random() - 0.5) * 0.5,
      color: Math.random() > 0.5 ? '#D4AF37' : '#FFD700',
      opacity: Math.random() * 0.5 + 0.1
    }))
    
    function resize() {
      w = canvas.width = window.innerWidth
      h = canvas.height = window.innerHeight
    }
    
    window.addEventListener('resize', resize)
    
    let raf
    function draw() {
      ctx.clearRect(0, 0, w, h)
      
      // Fond dégradé luxueux
      const gradient = ctx.createRadialGradient(w/2, h/2, 0, w/2, h/2, Math.max(w, h))
      gradient.addColorStop(0, '#0A0A0A')
      gradient.addColorStop(0.5, '#1A1A1A')
      gradient.addColorStop(1, '#2A2A2A')
      ctx.fillStyle = gradient
      ctx.fillRect(0, 0, w, h)
      
      // Particules dorées
      for (const p of goldenParticles) {
        p.x += p.speedX
        p.y += p.speedY
        
        if (p.x < 0) p.x = w
        if (p.x > w) p.x = 0
        if (p.y < 0) p.y = h
        if (p.y > h) p.y = 0
        
        ctx.beginPath()
        ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2)
        ctx.fillStyle = p.color
        ctx.globalAlpha = p.opacity
        ctx.fill()
      }
      
      // Étoiles avec effet de lueur
      for (const s of stars) {
        s.a = Math.sin(Date.now() * 0.001 + s.x * 0.01) * 0.3 + 0.5
        s.y += s.speed * 0.15
        if (s.y > h) {
          s.y = -10
          s.x = Math.random() * w
        }
        
        const alpha = Math.max(0.2, Math.min(1, s.a))
        ctx.globalAlpha = alpha
        
        // Effet de lueur
        const glowGradient = ctx.createRadialGradient(s.x, s.y, 0, s.x, s.y, s.glow)
        glowGradient.addColorStop(0, '#FFFFFF')
        glowGradient.addColorStop(0.3, '#FFD700')
        glowGradient.addColorStop(1, 'transparent')
        
        ctx.fillStyle = glowGradient
        ctx.beginPath()
        ctx.arc(s.x, s.y, s.glow, 0, Math.PI * 2)
        ctx.fill()
        
        // Étoile
        ctx.fillStyle = '#FFD700'
        ctx.beginPath()
        ctx.arc(s.x, s.y, s.r, 0, Math.PI * 2)
        ctx.fill()
      }
      
      ctx.globalAlpha = 1
      raf = requestAnimationFrame(draw)
    }
    
    draw()
    
    return () => {
      cancelAnimationFrame(raf)
      window.removeEventListener('resize', resize)
    }
  }, [])
  
  return (
    <canvas 
      ref={ref}
      className="stars-canvas"
      aria-hidden="true"
    />
  )
}

/* ---------------- Navigation Luxueuse ---------------- */
function Navigation() {
  const [scrolled, setScrolled] = useState(false)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  
  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50)
    }
    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])
  
  return (
    <header className={`main-header ${scrolled ? 'scrolled' : ''}`}>
      <div className="container nav-container">
        <Link to="/" className="logo">FEDERARIS</Link>
        
        <button 
          className="mobile-menu-btn"
          onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          style={{
            display: 'none',
            background: 'transparent',
            border: 'none',
            color: '#D4AF37',
            fontSize: '1.5rem',
            cursor: 'pointer'
          }}
        >
          {mobileMenuOpen ? '✕' : '☰'}
        </button>
        
        <nav className={`nav-menu ${mobileMenuOpen ? 'mobile-open' : ''}`}>
          <Link to="/" className="nav-link" onClick={() => setMobileMenuOpen(false)}>
            Accueil
          </Link>
          <Link to="/prestations" className="nav-link" onClick={() => setMobileMenuOpen(false)}>
            Prestations
          </Link>
          <Link to="/realisations" className="nav-link" onClick={() => setMobileMenuOpen(false)}>
            Réalisations
          </Link>
          <Link to="/expertise" className="nav-link" onClick={() => setMobileMenuOpen(false)}>
            Expertise
          </Link>
          <Link to="/contact" className="nav-cta" onClick={() => setMobileMenuOpen(false)}>
            Contact Exclusif
          </Link>
        </nav>
      </div>
    </header>
  )
}

/* ---------------- Hero Section - Écran d'Accueil Spectaculaire ---------------- */
function HeroSection() {
  return (
    <section className="hero-section">
      <div className="container">
        <div className="hero-content">
          <div className="hero-badge">
            <i className="fas fa-crown"></i> Excellence Inégalée
          </div>
          
          <h1 className="hero-title">
            L'Art de l'
            <span style={{ 
              display: 'block',
              background: 'linear-gradient(45deg, #D4AF37, #FFD700, #F5E7A1)',
              WebkitBackgroundClip: 'text',
              backgroundClip: 'text',
              color: 'transparent'
            }}>
              Exceptionnel
            </span>
          </h1>
          
          <p className="hero-subtitle">
            Federaris redéfinit les standards du luxe digital. Nous créons des expériences 
            qui transcendent l'attente et marquent les esprits pour l'éternité.
          </p>
          
          <div className="hero-buttons">
            <a href="#contact" className="btn btn-primary">
              <i className="fas fa-gem"></i>
              Découvrir l'Excellence
            </a>
            <a href="#prestations" className="btn btn-secondary">
              <i className="fas fa-play-circle"></i>
              Voir Nos Créations
            </a>
          </div>
          
          <div className="hero-stats">
            <div className="stat-item">
              <div className="stat-number">99.7%</div>
              <div className="stat-label">Satisfaction Client</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">150+</div>
              <div className="stat-label">Projets Exceptionnels</div>
            </div>
            <div className="stat-item">
              <div className="stat-number">24/7</div>
              <div className="stat-label">Support Premium</div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

/* ---------------- Prestations d'Excellence ---------------- */
function ServicesSection() {
  const services = [
    {
      icon: 'fas fa-crown',
      title: 'Branding Royal',
      description: 'Identité visuelle qui impose le respect et définit les nouveaux standards de luxe.',
      features: ['Logo Monumental', 'Charte Graphique Exclusive', 'Storytelling Épique']
    },
    {
      icon: 'fas fa-gem',
      title: 'Web Design Sur-Mesure',
      description: 'Interfaces qui captivent, impressionnent et convertissent l\'élite.',
      features: ['UX/UI Premium', 'Animations Cinématiques', 'Performance Maximale']
    },
    {
      icon: 'fas fa-chess-queen',
      title: 'Stratégie Digitale',
      description: 'Campagnes qui dominent les marchés et établissent l\'autorité.',
      features: ['Positionnement Exclusif', 'ROI Garanti', 'Analyse Prédictive']
    },
    {
      icon: 'fas fa-landmark',
      title: 'Expérience Client',
      description: 'Parcours client qui transforme chaque interaction en moment mémorable.',
      features: ['Onboarding Premium', 'Support Concierge', 'Fidélisation Exclusive']
    }
  ]
  
  return (
    <section id="prestations" className="section">
      <div className="container">
        <h2 className="section-title">Prestisations d'Exception</h2>
        <p className="section-subtitle">
          Nous ne créons pas des projets, nous bâtissons des légendes digitales.
        </p>
        
        <div className="features-grid">
          {services.map((service, index) => (
            <div key={index} className="feature-card">
              <div className="feature-icon">
                <i className={service.icon}></i>
              </div>
              <h3 className="feature-title">{service.title}</h3>
              <p className="feature-desc">{service.description}</p>
              <ul style={{
                marginTop: '1.5rem',
                listStyle: 'none',
                padding: 0
              }}>
                {service.features.map((feature, idx) => (
                  <li key={idx} style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.8rem',
                    marginBottom: '0.8rem',
                    fontSize: '0.95rem'
                  }}>
                    <i className="fas fa-check" style={{ color: '#D4AF37' }}></i>
                    {feature}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

/* ---------------- Réalisations Marquantes ---------------- */
function PortfolioSection() {
  const [activeProject, setActiveProject] = useState(0)
  
  const projects = [
    {
      title: 'Palais Royal Digital',
      client: 'Maison de Luxe Parisienne',
      description: 'Plateforme e-commerce révolutionnaire avec réalité augmentée.',
      results: ['+300% de conversion', 'Prix du Design 2023', 'Clientèle internationale'],
      image: 'https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?q=80&w=1500'
    },
    {
      title: 'Citadelle Financière',
      client: 'Institution Bancaire Privée',
      description: 'Système sécurisé de gestion de patrimoine pour ultra-riches.',
      results: ['100% de sécurité', 'Interface révolutionnaire', 'Adoption instantanée'],
      image: 'https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?q=80&w=1500'
    },
    {
      title: 'Jardin des Hespérides',
      client: 'Groupe Hôtelier 5 Étoiles',
      description: 'Expérience de réservation immersive avec visite virtuelle.',
      results: ['Occupancy +85%', 'Satisfaction 99%', 'Prix Innovation'],
      image: 'https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?q=80&w=1500'
    }
  ]
  
  return (
    <section className="section" style={{
      background: 'linear-gradient(180deg, rgba(10,10,10,0.5), rgba(26,26,26,0.8))'
    }}>
      <div className="container">
        <h2 className="section-title">Légendes Créées</h2>
        <p className="section-subtitle">
          Des réalisations qui redéfinissent les standards et inspirent l'industrie.
        </p>
        
        <div style={{
          display: 'grid',
          gridTemplateColumns: '1fr 2fr',
          gap: '4rem',
          alignItems: 'start'
        }}>
          <div>
            <div style={{
              display: 'flex',
              flexDirection: 'column',
              gap: '1rem'
            }}>
              {projects.map((project, index) => (
                <button
                  key={index}
                  onClick={() => setActiveProject(index)}
                  style={{
                    background: activeProject === index 
                      ? 'linear-gradient(45deg, rgba(139,69,19,0.3), rgba(184,115,51,0.2))'
                      : 'transparent',
                    border: '1px solid rgba(212,175,55,0.2)',
                    borderRadius: '15px',
                    padding: '1.5rem',
                    textAlign: 'left',
                    cursor: 'pointer',
                    transition: 'all 0.3s ease',
                    color: 'white'
                  }}
                >
                  <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '1rem',
                    marginBottom: '0.5rem'
                  }}>
                    <div style={{
                      width: '40px',
                      height: '40px',
                      background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                      borderRadius: '50%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '1.2rem'
                    }}>
                      {index + 1}
                    </div>
                    <div>
                      <div style={{ fontWeight: '700', fontSize: '1.2rem' }}>
                        {project.title}
                      </div>
                      <div style={{ color: '#D4AF37', fontSize: '0.9rem' }}>
                        {project.client}
                      </div>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          </div>
          
          <div style={{
            background: 'rgba(255,255,255,0.03)',
            border: '1px solid rgba(212,175,55,0.2)',
            borderRadius: '20px',
            overflow: 'hidden',
            boxShadow: '0 20px 60px rgba(0,0,0,0.5)'
          }}>
            <div style={{
              height: '300px',
              background: `url(${projects[activeProject].image}) center/cover`,
              position: 'relative'
            }}>
              <div style={{
                position: 'absolute',
                bottom: '20px',
                left: '30px',
                right: '30px',
                background: 'rgba(0,0,0,0.7)',
                padding: '1.5rem',
                borderRadius: '15px',
                backdropFilter: 'blur(10px)'
              }}>
                <h3 style={{
                  fontSize: '1.8rem',
                  marginBottom: '0.5rem',
                  background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                  WebkitBackgroundClip: 'text',
                  backgroundClip: 'text',
                  color: 'transparent'
                }}>
                  {projects[activeProject].title}
                </h3>
                <p style={{ color: 'rgba(255,255,255,0.8)', marginBottom: '1rem' }}>
                  {projects[activeProject].description}
                </p>
                <div style={{
                  display: 'flex',
                  gap: '1rem',
                  flexWrap: 'wrap'
                }}>
                  {projects[activeProject].results.map((result, idx) => (
                    <span key={idx} style={{
                      background: 'rgba(212,175,55,0.1)',
                      border: '1px solid rgba(212,175,55,0.3)',
                      padding: '0.5rem 1rem',
                      borderRadius: '20px',
                      fontSize: '0.9rem',
                      color: '#FFD700'
                    }}>
                      {result}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

/* ---------------- Contact Exclusif ---------------- */
function ContactSection() {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    company: '',
    message: ''
  })
  
  const handleSubmit = (e) => {
    e.preventDefault()
    alert('Votre demande exclusive a été transmise. Notre équipe vous contactera sous 24h.')
  }
  
  return (
    <section id="contact" className="section">
      <div className="container">
        <div style={{
          background: 'linear-gradient(135deg, rgba(10,10,10,0.8), rgba(26,26,26,0.9))',
          border: '1px solid rgba(212,175,55,0.3)',
          borderRadius: '30px',
          padding: '4rem',
          position: 'relative',
          overflow: 'hidden'
        }}>
          <div style={{
            position: 'absolute',
            top: '-100px',
            right: '-100px',
            width: '300px',
            height: '300px',
            background: 'radial-gradient(circle, rgba(212,175,55,0.1), transparent 70%)',
            borderRadius: '50%'
          }}></div>
          
          <div style={{
            maxWidth: '800px',
            margin: '0 auto'
          }}>
            <h2 className="section-title" style={{ fontSize: '3rem' }}>
              Accédez à l'Excellence
            </h2>
            <p className="section-subtitle" style={{ fontSize: '1.3rem' }}>
              Votre projet mérite l'exceptionnel. Partagez-nous votre vision.
            </p>
            
            <form onSubmit={handleSubmit} style={{
              marginTop: '3rem',
              display: 'grid',
              gap: '2rem'
            }}>
              <div style={{
                display: 'grid',
                gridTemplateColumns: '1fr 1fr',
                gap: '2rem'
              }}>
                <div>
                  <label style={{
                    display: 'block',
                    marginBottom: '0.5rem',
                    color: '#D4AF37',
                    fontWeight: '600'
                  }}>
                    Nom & Prénom
                  </label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({...formData, name: e.target.value})}
                    style={{
                      width: '100%',
                      padding: '1rem',
                      background: 'rgba(255,255,255,0.05)',
                      border: '1px solid rgba(212,175,55,0.3)',
                      borderRadius: '10px',
                      color: 'white',
                      fontSize: '1rem'
                    }}
                    required
                  />
                </div>
                <div>
                  <label style={{
                    display: 'block',
                    marginBottom: '0.5rem',
                    color: '#D4AF37',
                    fontWeight: '600'
                  }}>
                    Société
                  </label>
                  <input
                    type="text"
                    value={formData.company}
                    onChange={(e) => setFormData({...formData, company: e.target.value})}
                    style={{
                      width: '100%',
                      padding: '1rem',
                      background: 'rgba(255,255,255,0.05)',
                      border: '1px solid rgba(212,175,55,0.3)',
                      borderRadius: '10px',
                      color: 'white',
                      fontSize: '1rem'
                    }}
                    required
                  />
                </div>
              </div>
              
              <div>
                <label style={{
                  display: 'block',
                  marginBottom: '0.5rem',
                  color: '#D4AF37',
                  fontWeight: '600'
                }}>
                  Email Professionnel
                </label>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({...formData, email: e.target.value})}
                  style={{
                    width: '100%',
                    padding: '1rem',
                    background: 'rgba(255,255,255,0.05)',
                    border: '1px solid rgba(212,175,55,0.3)',
                    borderRadius: '10px',
                    color: 'white',
                    fontSize: '1rem'
                  }}
                  required
                />
              </div>
              
              <div>
                <label style={{
                  display: 'block',
                  marginBottom: '0.5rem',
                  color: '#D4AF37',
                  fontWeight: '600'
                }}>
                  Votre Vision
                </label>
                <textarea
                  value={formData.message}
                  onChange={(e) => setFormData({...formData, message: e.target.value})}
                  rows="5"
                  style={{
                    width: '100%',
                    padding: '1rem',
                    background: 'rgba(255,255,255,0.05)',
                    border: '1px solid rgba(212,175,55,0.3)',
                    borderRadius: '10px',
                    color: 'white',
                    fontSize: '1rem',
                    resize: 'vertical'
                  }}
                  required
                />
              </div>
              
              <button
                type="submit"
                style={{
                  background: 'linear-gradient(45deg, #8B4513, #B87333, #D4AF37)',
                  color: 'white',
                  border: 'none',
                  padding: '1.5rem 3rem',
                  borderRadius: '50px',
                  fontSize: '1.1rem',
                  fontWeight: '700',
                  cursor: 'pointer',
                  transition: 'all 0.3s ease',
                  width: '100%',
                  maxWidth: '300px',
                  margin: '0 auto',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '1rem'
                }}
              >
                <i className="fas fa-paper-plane"></i>
                Accéder à l'Excellence
              </button>
            </form>
            
            <div style={{
              marginTop: '3rem',
              paddingTop: '3rem',
              borderTop: '1px solid rgba(212,175,55,0.2)',
              textAlign: 'center',
              color: 'rgba(255,255,255,0.6)',
              fontSize: '0.9rem'
            }}>
              <i className="fas fa-shield-alt" style={{ marginRight: '0.5rem', color: '#D4AF37' }}></i>
              Confidentialité absolue • Réponse sous 24h • Accompagnement VIP
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

/* ---------------- Footer ---------------- */
function Footer() {
  return (
    <footer className="main-footer">
      <div className="container">
        <div className="footer-content">
          <div>
            <div className="footer-logo">FEDERARIS</div>
            <p style={{ marginTop: '1rem', opacity: 0.7 }}>
              Redéfinir l'excellence, créer des légendes.
            </p>
            <div style={{ marginTop: '2rem', display: 'flex', gap: '1rem' }}>
              <a href="#" style={{ color: '#D4AF37', fontSize: '1.2rem' }}>
                <i className="fab fa-instagram"></i>
              </a>
              <a href="#" style={{ color: '#D4AF37', fontSize: '1.2rem' }}>
                <i className="fab fa-linkedin"></i>
              </a>
              <a href="#" style={{ color: '#D4AF37', fontSize: '1.2rem' }}>
                <i className="fab fa-behance"></i>
              </a>
            </div>
          </div>
          
          <div>
            <h4 style={{ marginBottom: '1.5rem', color: '#D4AF37' }}>Expertise</h4>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              <li style={{ marginBottom: '0.8rem' }}>
                <a href="#" style={{ color: 'rgba(255,255,255,0.7)', textDecoration: 'none' }}>
                  Stratégie de Marque
                </a>
              </li>
              <li style={{ marginBottom: '0.8rem' }}>
                <a href="#" style={{ color: 'rgba(255,255,255,0.7)', textDecoration: 'none' }}>
                  Design d'Exception
                </a>
              </li>
              <li style={{ marginBottom: '0.8rem' }}>
                <a href="#" style={{ color: 'rgba(255,255,255,0.7)', textDecoration: 'none' }}>
                  Innovation Digitale
                </a>
              </li>
            </ul>
          </div>
          
          <div>
            <h4 style={{ marginBottom: '1.5rem', color: '#D4AF37' }}>Contact</h4>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              <li style={{ marginBottom: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.8rem' }}>
                <i className="fas fa-map-marker-alt" style={{ color: '#D4AF37' }}></i>
                <span style={{ color: 'rgba(255,255,255,0.7)' }}>Paris • Monaco • Genève</span>
              </li>
              <li style={{ marginBottom: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.8rem' }}>
                <i className="fas fa-envelope" style={{ color: '#D4AF37' }}></i>
                <span style={{ color: 'rgba(255,255,255,0.7)' }}>contact@federaris.com</span>
              </li>
              <li style={{ marginBottom: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.8rem' }}>
                <i className="fas fa-phone" style={{ color: '#D4AF37' }}></i>
                <span style={{ color: 'rgba(255,255,255,0.7)' }}>+33 1 84 88 88 88</span>
              </li>
            </ul>
          </div>
        </div>
        
        <div className="footer-copyright">
          © 2024 Federaris. Tous droits réservés. L'excellence n'a pas de prix.
        </div>
      </div>
    </footer>
  )
}

/* ---------------- App Principal ---------------- */
function App() {
  return (
    <Router>
      <div style={{ position: 'relative' }}>
        <StarsCanvas />
        <Navigation />
        <main>
          <Routes>
            <Route path="/" element={
              <>
                <HeroSection />
                <ServicesSection />
                <PortfolioSection />
                <ContactSection />
                <Footer />
              </>
            } />
          </Routes>
        </main>
      </div>
    </Router>
  )
}

export default App