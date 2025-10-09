import React, { useState } from "react";
import PageLoader from "./PageLoader";
import SectionLoader from "./SectionLoader";

const LoaderShowcase: React.FC = () => {
  const [showPageLoader, setShowPageLoader] = useState(false);

  return (
    <div style={{ padding: "2rem", maxWidth: "1200px", margin: "0 auto" }}>
      <h1 style={{ marginBottom: "2rem", color: "#667eea" }}>
        PMC Animated Loaders - Showcase
      </h1>

      {/* Page Loader Demo */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>1. PageLoader</h2>
        <p style={{ marginBottom: "1rem", color: "#666" }}>
          Full-screen loader with animated PMC logo, rotating rings, floating
          particles, and progress bar.
        </p>

        <button
          onClick={() => setShowPageLoader(true)}
          className="pmc-button pmc-button-primary"
          style={{ marginBottom: "1rem" }}
        >
          Show PageLoader Demo (5 seconds)
        </button>

        {showPageLoader && <PageLoader message="Demo: Loading Dashboard..." />}

        {showPageLoader &&
          setTimeout(() => setShowPageLoader(false), 5000) &&
          null}

        <div className="pmc-card" style={{ marginTop: "1rem" }}>
          <div className="pmc-card-header">
            <h3>Usage Examples</h3>
          </div>
          <div className="pmc-card-body">
            <pre
              style={{
                background: "#f5f5f5",
                padding: "1rem",
                borderRadius: "4px",
                overflow: "auto",
              }}
            >
              {`// Default
<PageLoader />

// With message
<PageLoader message="Loading Dashboard..." />

// Non-fullscreen (for modals)
<PageLoader message="Processing..." fullScreen={false} />`}
            </pre>
          </div>
        </div>
      </section>

      {/* Section Loader - Default Variant */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>
          2. SectionLoader - Default Variant
        </h2>
        <p style={{ marginBottom: "1rem", color: "#666" }}>
          Animated PMC logo with rotating ring. Best for important sections.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
            gap: "1.5rem",
          }}
        >
          {/* Small */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Small Size</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "150px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <SectionLoader size="small" message="Loading..." />
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  size="small" 
  message="Loading..." 
/>`}
              </pre>
            </div>
          </div>

          {/* Medium */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Medium Size (Default)</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "150px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <SectionLoader message="Loading data..." />
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  message="Loading data..." 
/>`}
              </pre>
            </div>
          </div>

          {/* Large */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Large Size</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "200px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <SectionLoader size="large" message="Loading applications..." />
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  size="large" 
  message="Loading..." 
/>`}
              </pre>
            </div>
          </div>
        </div>
      </section>

      {/* Section Loader - Minimal Variant */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>
          3. SectionLoader - Minimal Variant
        </h2>
        <p style={{ marginBottom: "1rem", color: "#666" }}>
          Simple spinner. Lightweight for quick actions.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
            gap: "1.5rem",
          }}
        >
          {/* Small */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Small Minimal</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "120px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <SectionLoader
                variant="minimal"
                size="small"
                message="Loading..."
              />
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  variant="minimal" 
  size="small" 
/>`}
              </pre>
            </div>
          </div>

          {/* Medium */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Medium Minimal</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "120px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <SectionLoader variant="minimal" message="Processing..." />
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  variant="minimal" 
  message="Processing..." 
/>`}
              </pre>
            </div>
          </div>

          {/* Inline */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>Inline Minimal</h4>
            </div>
            <div
              className="pmc-card-body"
              style={{
                minHeight: "120px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <div
                style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}
              >
                <SectionLoader variant="minimal" size="small" inline />
                <span>Saving changes...</span>
              </div>
            </div>
            <div className="pmc-card-footer">
              <pre style={{ fontSize: "0.85rem" }}>
                {`<SectionLoader 
  variant="minimal" 
  size="small" 
  inline 
/>`}
              </pre>
            </div>
          </div>
        </div>
      </section>

      {/* Section Loader - Skeleton Variant */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>
          4. SectionLoader - Skeleton Variant
        </h2>
        <p style={{ marginBottom: "1rem", color: "#666" }}>
          Content placeholder with shimmer effect. Best UX for content areas.
        </p>

        <div className="pmc-card">
          <div className="pmc-card-header">
            <h4>Skeleton Loader</h4>
          </div>
          <div className="pmc-card-body" style={{ minHeight: "150px" }}>
            <SectionLoader variant="skeleton" />
          </div>
          <div className="pmc-card-footer">
            <pre style={{ fontSize: "0.85rem" }}>
              {`<SectionLoader variant="skeleton" />`}
            </pre>
          </div>
        </div>
      </section>

      {/* Use Case Examples */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>5. Real-World Use Cases</h2>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
            gap: "1.5rem",
          }}
        >
          {/* Table Loading */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>📊 Table Loading</h4>
            </div>
            <div className="pmc-card-body">
              <table className="pmc-table" style={{ marginBottom: "1rem" }}>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Name</th>
                    <th>Status</th>
                  </tr>
                </thead>
              </table>
              <SectionLoader message="Loading applications..." />
            </div>
            <div className="pmc-card-footer">
              <small style={{ color: "#666" }}>
                Use: Default variant, medium/large size
              </small>
            </div>
          </div>

          {/* Button Loading */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>🔘 Button Loading</h4>
            </div>
            <div className="pmc-card-body">
              <button
                className="pmc-button pmc-button-primary"
                style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}
                disabled
              >
                <SectionLoader variant="minimal" size="small" inline />
                Submitting...
              </button>
            </div>
            <div className="pmc-card-footer">
              <small style={{ color: "#666" }}>
                Use: Minimal variant, small size, inline
              </small>
            </div>
          </div>

          {/* Card Content */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>📄 Card Content</h4>
            </div>
            <div className="pmc-card-body" style={{ minHeight: "120px" }}>
              <SectionLoader variant="skeleton" />
            </div>
            <div className="pmc-card-footer">
              <small style={{ color: "#666" }}>
                Use: Skeleton variant for better UX
              </small>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>✨ Features</h2>

        <div className="pmc-card">
          <div className="pmc-card-body">
            <ul style={{ listStyle: "none", padding: 0 }}>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Premium Animations</strong> - Rotating rings,
                floating logo, particles
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>3 Variants</strong> - Default (rich), Minimal
                (simple), Skeleton (placeholder)
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>3 Sizes</strong> - Small (60px), Medium (80px), Large
                (120px)
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Responsive Design</strong> - Works on all screen
                sizes
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Dark Mode Support</strong> - Automatic theme
                detection
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Accessibility</strong> - Respects
                prefers-reduced-motion
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Performance</strong> - GPU-accelerated CSS animations
              </li>
              <li style={{ marginBottom: "0.5rem" }}>
                ✅ <strong>Brand Consistent</strong> - PMC logo and colors
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* Props Reference */}
      <section style={{ marginBottom: "3rem" }}>
        <h2 style={{ marginBottom: "1rem" }}>📖 Props Reference</h2>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(400px, 1fr))",
            gap: "1.5rem",
          }}
        >
          {/* PageLoader Props */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>PageLoader Props</h4>
            </div>
            <div className="pmc-card-body">
              <table style={{ width: "100%", fontSize: "0.9rem" }}>
                <thead>
                  <tr style={{ borderBottom: "2px solid #e5e7eb" }}>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Prop
                    </th>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Type
                    </th>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Default
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>message</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>string</td>
                    <td style={{ padding: "0.5rem" }}>"Loading..."</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>fullScreen</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>boolean</td>
                    <td style={{ padding: "0.5rem" }}>true</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* SectionLoader Props */}
          <div className="pmc-card">
            <div className="pmc-card-header">
              <h4>SectionLoader Props</h4>
            </div>
            <div className="pmc-card-body">
              <table style={{ width: "100%", fontSize: "0.9rem" }}>
                <thead>
                  <tr style={{ borderBottom: "2px solid #e5e7eb" }}>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Prop
                    </th>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Type
                    </th>
                    <th style={{ textAlign: "left", padding: "0.5rem" }}>
                      Default
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>message</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>string</td>
                    <td style={{ padding: "0.5rem" }}>"Loading..."</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>size</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>
                      small | medium | large
                    </td>
                    <td style={{ padding: "0.5rem" }}>"medium"</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>variant</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>
                      default | minimal | skeleton
                    </td>
                    <td style={{ padding: "0.5rem" }}>"default"</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "0.5rem" }}>
                      <code>inline</code>
                    </td>
                    <td style={{ padding: "0.5rem" }}>boolean</td>
                    <td style={{ padding: "0.5rem" }}>false</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <div
        style={{
          textAlign: "center",
          padding: "2rem",
          color: "#666",
          borderTop: "1px solid #e5e7eb",
          marginTop: "3rem",
        }}
      >
        <p>
          <strong>PMC Animated Loaders</strong> - Professional loading
          experience for your application
        </p>
        <p style={{ fontSize: "0.9rem", marginTop: "0.5rem" }}>
          See <code>LOADER_USAGE_GUIDE.md</code> for detailed documentation
        </p>
      </div>
    </div>
  );
};

export default LoaderShowcase;
