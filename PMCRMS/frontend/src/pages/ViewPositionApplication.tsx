import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  FileText,
  CheckCircle,
  XCircle,
} from "lucide-react";
import positionRegistrationService, {
  type PositionRegistrationResponse,
} from "../services/positionRegistrationService";
import { PageLoader } from "../components";

const ViewPositionApplication: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [application, setApplication] =
    useState<PositionRegistrationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchApplication = async () => {
      if (!id) {
        setError("Application ID is required");
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        setApplication(response);
      } catch (err) {
        console.error("Error fetching application:", err);
        setError("Failed to load application details");
      } finally {
        setLoading(false);
      }
    };

    fetchApplication();
  }, [id]);

  if (loading) {
    return <PageLoader message="Loading application details..." />;
  }

  if (error || !application) {
    return (
      <div className="pmc-container" style={{ padding: "40px 20px" }}>
        <div className="pmc-card">
          <div
            className="pmc-card-body"
            style={{ textAlign: "center", padding: "40px" }}
          >
            <XCircle size={48} color="#dc2626" style={{ margin: "0 auto" }} />
            <h2 style={{ marginTop: "16px", color: "#dc2626" }}>Error</h2>
            <p style={{ color: "#64748b", marginTop: "8px" }}>{error}</p>
            <button
              onClick={() => navigate("/dashboard")}
              className="pmc-button pmc-button-primary"
              style={{ marginTop: "24px" }}
            >
              <ArrowLeft size={16} style={{ marginRight: "8px" }} />
              Back to Dashboard
            </button>
          </div>
        </div>
      </div>
    );
  }

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 1: // Draft
        return <span className="pmc-badge pmc-status-pending">Draft</span>;
      case 2: // Submitted
        return (
          <span className="pmc-badge pmc-status-under-review">Submitted</span>
        );
      case 23: // Completed
        return <span className="pmc-badge pmc-status-approved">Completed</span>;
      default:
        return (
          <span className="pmc-badge pmc-status-under-review">
            Under Review
          </span>
        );
    }
  };

  return (
    <div className="pmc-container" style={{ padding: "20px" }}>
      {/* Header */}
      <div style={{ marginBottom: "24px" }}>
        <button
          onClick={() => navigate("/dashboard")}
          className="pmc-button pmc-button-secondary pmc-button-sm"
          style={{ marginBottom: "16px" }}
        >
          <ArrowLeft size={16} style={{ marginRight: "8px" }} />
          Back to Dashboard
        </button>

        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
          }}
        >
          <div>
            <h1 className="pmc-page-title">Application Details</h1>
            {application.applicationNumber && (
              <p
                style={{ color: "#64748b", fontSize: "14px", marginTop: "4px" }}
              >
                Application #: <strong>{application.applicationNumber}</strong>
              </p>
            )}
          </div>
          {getStatusBadge(application.status)}
        </div>
      </div>

      {/* Basic Information */}
      <div className="pmc-card" style={{ marginBottom: "16px" }}>
        <div
          className="pmc-card-header"
          style={{
            background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
            color: "#334155",
            padding: "12px 16px",
            borderBottom: "2px solid #cbd5e1",
          }}
        >
          <h2
            className="pmc-card-title"
            style={{ color: "#334155", margin: 0 }}
          >
            Basic Information
          </h2>
        </div>
        <div className="pmc-card-body">
          <div className="pmc-form-grid pmc-form-grid-3">
            <div>
              <label className="pmc-label">Position Type</label>
              <p className="pmc-value">{application.positionTypeName}</p>
            </div>
            <div>
              <label className="pmc-label">Full Name</label>
              <p className="pmc-value">{application.fullName}</p>
            </div>
            <div>
              <label className="pmc-label">Mother's Name</label>
              <p className="pmc-value">{application.motherName}</p>
            </div>
            <div>
              <label className="pmc-label">Mobile Number</label>
              <p className="pmc-value">{application.mobileNumber}</p>
            </div>
            <div>
              <label className="pmc-label">Email Address</label>
              <p className="pmc-value">{application.emailAddress}</p>
            </div>
            <div>
              <label className="pmc-label">Gender</label>
              <p className="pmc-value">{application.genderName}</p>
            </div>
            <div>
              <label className="pmc-label">Date of Birth</label>
              <p className="pmc-value">
                {new Date(application.dateOfBirth).toLocaleDateString()} (
                {application.age} years)
              </p>
            </div>
            {application.bloodGroup && (
              <div>
                <label className="pmc-label">Blood Group</label>
                <p className="pmc-value">{application.bloodGroup}</p>
              </div>
            )}
            {application.height && (
              <div>
                <label className="pmc-label">Height</label>
                <p className="pmc-value">{application.height} cm</p>
              </div>
            )}
            <div>
              <label className="pmc-label">PAN Card Number</label>
              <p className="pmc-value">{application.panCardNumber}</p>
            </div>
            <div>
              <label className="pmc-label">Aadhar Card Number</label>
              <p className="pmc-value">{application.aadharCardNumber}</p>
            </div>
            {application.coaCardNumber && (
              <div>
                <label className="pmc-label">COA Card Number</label>
                <p className="pmc-value">{application.coaCardNumber}</p>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Addresses */}
      {application.addresses.map((address) => (
        <div
          className="pmc-card"
          style={{ marginBottom: "16px" }}
          key={address.id}
        >
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              {address.addressType} Address
            </h2>
          </div>
          <div className="pmc-card-body">
            <p className="pmc-value">{address.fullAddress}</p>
          </div>
        </div>
      ))}

      {/* Qualifications */}
      {application.qualifications.length > 0 && (
        <div className="pmc-card" style={{ marginBottom: "16px" }}>
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Educational Qualifications
            </h2>
          </div>
          <div className="pmc-card-body">
            {application.qualifications.map((qual, index) => (
              <div
                key={qual.id}
                style={{
                  padding: "16px",
                  background: "#f8fafc",
                  borderRadius: "8px",
                  marginBottom:
                    index < application.qualifications.length - 1
                      ? "12px"
                      : "0",
                  border: "1px solid #e2e8f0",
                }}
              >
                <div className="pmc-form-grid pmc-form-grid-3">
                  <div>
                    <label className="pmc-label">Institute Name</label>
                    <p className="pmc-value">{qual.instituteName}</p>
                  </div>
                  <div>
                    <label className="pmc-label">University Name</label>
                    <p className="pmc-value">{qual.universityName}</p>
                  </div>
                  <div>
                    <label className="pmc-label">Degree/Specialization</label>
                    <p className="pmc-value">
                      {qual.degreeName} ({qual.specializationName})
                    </p>
                  </div>
                  <div>
                    <label className="pmc-label">Passing Month & Year</label>
                    <p className="pmc-value">
                      {qual.passingMonthName} {qual.yearOfPassing}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Experiences */}
      {application.experiences.length > 0 && (
        <div className="pmc-card" style={{ marginBottom: "16px" }}>
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Work Experience
            </h2>
          </div>
          <div className="pmc-card-body">
            {application.experiences.map((exp, index) => (
              <div
                key={exp.id}
                style={{
                  padding: "16px",
                  background: "#f8fafc",
                  borderRadius: "8px",
                  marginBottom:
                    index < application.experiences.length - 1 ? "12px" : "0",
                  border: "1px solid #e2e8f0",
                }}
              >
                <div className="pmc-form-grid pmc-form-grid-3">
                  <div>
                    <label className="pmc-label">Company Name</label>
                    <p className="pmc-value">{exp.companyName}</p>
                  </div>
                  <div>
                    <label className="pmc-label">Position</label>
                    <p className="pmc-value">{exp.position}</p>
                  </div>
                  <div>
                    <label className="pmc-label">Duration</label>
                    <p className="pmc-value">
                      {new Date(exp.fromDate).toLocaleDateString()} -{" "}
                      {new Date(exp.toDate).toLocaleDateString()}
                    </p>
                  </div>
                  <div>
                    <label className="pmc-label">Total Experience</label>
                    <p className="pmc-value">{exp.yearsOfExperience} years</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Documents */}
      {application.documents.length > 0 && (
        <div className="pmc-card" style={{ marginBottom: "16px" }}>
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Uploaded Documents
            </h2>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-form-grid pmc-form-grid-2">
              {application.documents.map((doc) => (
                <div
                  key={doc.id}
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    border: "1px solid #e2e8f0",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                    }}
                  >
                    <FileText size={24} color="#3b82f6" />
                    <div>
                      <p className="pmc-value" style={{ marginBottom: "4px" }}>
                        {doc.documentTypeName}
                      </p>
                      <p style={{ fontSize: "12px", color: "#64748b" }}>
                        {doc.fileName}
                      </p>
                      {doc.fileSize && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          {(doc.fileSize / 1024).toFixed(2)} KB
                        </p>
                      )}
                    </div>
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    {doc.isVerified ? (
                      <span title="Verified">
                        <CheckCircle size={20} color="#10b981" />
                      </span>
                    ) : (
                      <span title="Not Verified">
                        <XCircle size={20} color="#94a3b8" />
                      </span>
                    )}
                    <button
                      className="pmc-button pmc-button-secondary pmc-button-sm"
                      onClick={() => window.open(doc.filePath, "_blank")}
                    >
                      <Download size={14} style={{ marginRight: "4px" }} />
                      View
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Application Timeline */}
      <div className="pmc-card">
        <div
          className="pmc-card-header"
          style={{
            background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
            color: "#334155",
            padding: "12px 16px",
            borderBottom: "2px solid #cbd5e1",
          }}
        >
          <h2
            className="pmc-card-title"
            style={{ color: "#334155", margin: 0 }}
          >
            Application Timeline
          </h2>
        </div>
        <div className="pmc-card-body">
          <div className="pmc-form-grid pmc-form-grid-3">
            <div>
              <label className="pmc-label">Created Date</label>
              <p className="pmc-value">
                {new Date(application.createdDate).toLocaleString()}
              </p>
            </div>
            {application.submittedDate && (
              <div>
                <label className="pmc-label">Submitted Date</label>
                <p className="pmc-value">
                  {new Date(application.submittedDate).toLocaleString()}
                </p>
              </div>
            )}
            {application.approvedDate && (
              <div>
                <label className="pmc-label">Approved Date</label>
                <p className="pmc-value">
                  {new Date(application.approvedDate).toLocaleString()}
                </p>
              </div>
            )}
          </div>
          {application.remarks && (
            <div style={{ marginTop: "16px" }}>
              <label className="pmc-label">Remarks</label>
              <p className="pmc-value">{application.remarks}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ViewPositionApplication;
