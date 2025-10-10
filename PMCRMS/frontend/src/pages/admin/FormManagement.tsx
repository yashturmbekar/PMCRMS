import React, { useEffect, useState } from "react";
import {
  adminService,
  type FormConfiguration,
  type CustomField,
} from "../../services/adminService";
import {
  PencilIcon,
  TrashIcon,
  PlusIcon,
  BanknotesIcon,
} from "@heroicons/react/24/outline";

const FormManagement: React.FC = () => {
  const [forms, setForms] = useState<FormConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showFeeModal, setShowFeeModal] = useState(false);
  const [showFieldsModal, setShowFieldsModal] = useState(false);
  const [selectedForm, setSelectedForm] = useState<FormConfiguration | null>(
    null
  );

  // Fee form state
  const [feeForm, setFeeForm] = useState({
    baseFee: 0,
    processingFee: 0,
    lateFee: 0,
    effectiveFrom: "",
    changeReason: "",
  });

  // Custom field form state
  const [newField, setNewField] = useState<CustomField>({
    fieldName: "",
    fieldType: "text",
    label: "",
    isRequired: false,
    options: [],
  });
  const [customFields, setCustomFields] = useState<CustomField[]>([]);

  useEffect(() => {
    loadForms();
  }, []);

  const loadForms = async () => {
    try {
      setLoading(true);
      setError("");
      const response = await adminService.getAllForms();
      if (response.success && response.data) {
        setForms(response.data);
      } else {
        setError(response.message || "Failed to load forms");
      }
    } catch (err) {
      console.error("Error loading forms:", err);
      setError("Failed to load forms");
    } finally {
      setLoading(false);
    }
  };

  const handleEditFees = (form: FormConfiguration) => {
    setSelectedForm(form);
    setFeeForm({
      baseFee: form.baseFee,
      processingFee: form.processingFee,
      lateFee: form.lateFee || 0,
      effectiveFrom: new Date().toISOString().split("T")[0],
      changeReason: "",
    });
    setShowFeeModal(true);
  };

  const handleUpdateFees = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedForm) return;

    try {
      const response = await adminService.updateFormFees(
        selectedForm.id,
        feeForm
      );
      if (response.success) {
        setSuccess("Form fees updated successfully!");
        setShowFeeModal(false);
        loadForms();
      } else {
        setError(response.message || "Failed to update fees");
      }
    } catch (err) {
      console.error("Error updating fees:", err);
      setError("Failed to update fees");
    }
  };

  const handleEditCustomFields = (form: FormConfiguration) => {
    setSelectedForm(form);
    setCustomFields(Array.isArray(form.customFields) ? form.customFields : []);
    setShowFieldsModal(true);
  };

  const handleAddCustomField = () => {
    if (!newField.fieldName || !newField.label) {
      alert("Please fill in field name and label");
      return;
    }

    setCustomFields([...customFields, newField]);
    setNewField({
      fieldName: "",
      fieldType: "text",
      label: "",
      isRequired: false,
      options: [],
    });
  };

  const handleRemoveCustomField = (index: number) => {
    setCustomFields(customFields.filter((_, i) => i !== index));
  };

  const handleSaveCustomFields = async () => {
    if (!selectedForm) return;

    try {
      const response = await adminService.updateFormCustomFields(
        selectedForm.id,
        JSON.stringify(customFields)
      );
      if (response.success) {
        setSuccess("Custom fields updated successfully!");
        setShowFieldsModal(false);
        loadForms();
      } else {
        setError(response.message || "Failed to update custom fields");
      }
    } catch (err) {
      console.error("Error updating custom fields:", err);
      setError("Failed to update custom fields");
    }
  };

  const handleDeleteForm = async (formId: number) => {
    if (
      !confirm(
        "Are you sure you want to delete this form? This action cannot be undone."
      )
    ) {
      return;
    }

    try {
      const response = await adminService.deleteForm(formId);
      if (response.success) {
        setSuccess("Form deleted successfully!");
        loadForms();
      } else {
        setError(response.message || "Failed to delete form");
      }
    } catch (err) {
      console.error("Error deleting form:", err);
      setError("Failed to delete form");
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
      maximumFractionDigits: 0,
    }).format(amount);
  };

  return (
    <div className="p-6 bg-gray-50">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-800">Form Management</h1>
        <p className="text-gray-600 mt-1">
          Manage form configurations, fees, and custom fields
        </p>
      </div>

      {/* Success/Error Messages */}
      {success && (
        <div className="mb-4 bg-green-50 border border-green-200 text-green-800 px-4 py-3 rounded-lg">
          {success}
          <button
            onClick={() => setSuccess("")}
            className="float-right font-bold"
          >
            ×
          </button>
        </div>
      )}
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-lg">
          {error}
          <button
            onClick={() => setError("")}
            className="float-right font-bold"
          >
            ×
          </button>
        </div>
      )}

      {/* Loading State */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6">
          {forms.length === 0 ? (
            <div className="bg-white shadow-md rounded-lg p-12 text-center text-gray-500">
              No forms available.
            </div>
          ) : (
            forms.map((form) => (
              <div
                key={form.id}
                className="bg-white shadow-md rounded-lg p-6 hover:shadow-lg transition-shadow"
              >
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h3 className="text-xl font-semibold text-gray-900">
                      {form.formName}
                    </h3>
                    <p className="text-gray-600 mt-1">{form.description}</p>

                    <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div className="bg-blue-50 p-3 rounded-lg">
                        <p className="text-sm text-gray-600">Base Fee</p>
                        <p className="text-lg font-semibold text-blue-900">
                          {formatCurrency(form.baseFee)}
                        </p>
                      </div>
                      <div className="bg-green-50 p-3 rounded-lg">
                        <p className="text-sm text-gray-600">Processing Fee</p>
                        <p className="text-lg font-semibold text-green-900">
                          {formatCurrency(form.processingFee)}
                        </p>
                      </div>
                      <div className="bg-yellow-50 p-3 rounded-lg">
                        <p className="text-sm text-gray-600">Late Fee</p>
                        <p className="text-lg font-semibold text-yellow-900">
                          {formatCurrency(form.lateFee || 0)}
                        </p>
                      </div>
                    </div>

                    <div className="mt-4 flex items-center gap-4">
                      <span
                        className={`px-3 py-1 rounded-full text-sm font-medium ${
                          form.isActive
                            ? "bg-green-100 text-green-800"
                            : "bg-gray-100 text-gray-800"
                        }`}
                      >
                        {form.isActive ? "Active" : "Inactive"}
                      </span>
                      {form.customFields &&
                        Array.isArray(form.customFields) && (
                          <span className="text-sm text-gray-600">
                            {form.customFields.length} custom fields
                          </span>
                        )}
                    </div>
                  </div>

                  <div className="flex flex-col gap-2 ml-4">
                    <button
                      onClick={() => handleEditFees(form)}
                      className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                      title="Edit Fees"
                    >
                      <BanknotesIcon className="h-5 w-5" />
                      Edit Fees
                    </button>
                    <button
                      onClick={() => handleEditCustomFields(form)}
                      className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                      title="Manage Custom Fields"
                    >
                      <PencilIcon className="h-5 w-5" />
                      Custom Fields
                    </button>
                    <button
                      onClick={() => handleDeleteForm(form.id)}
                      className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                      title="Delete Form"
                    >
                      <TrashIcon className="h-5 w-5" />
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {/* Edit Fees Modal */}
      {showFeeModal && selectedForm && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-[500px] shadow-lg rounded-md bg-white">
            <div className="mt-3">
              <h3 className="text-lg font-medium leading-6 text-gray-900 mb-4">
                Edit Fees - {selectedForm.formName}
              </h3>
              <form onSubmit={handleUpdateFees} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Base Fee (₹)
                  </label>
                  <input
                    type="number"
                    required
                    min="0"
                    step="0.01"
                    value={feeForm.baseFee}
                    onChange={(e) =>
                      setFeeForm({
                        ...feeForm,
                        baseFee: parseFloat(e.target.value),
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Processing Fee (₹)
                  </label>
                  <input
                    type="number"
                    required
                    min="0"
                    step="0.01"
                    value={feeForm.processingFee}
                    onChange={(e) =>
                      setFeeForm({
                        ...feeForm,
                        processingFee: parseFloat(e.target.value),
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Late Fee (₹)
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={feeForm.lateFee}
                    onChange={(e) =>
                      setFeeForm({
                        ...feeForm,
                        lateFee: parseFloat(e.target.value),
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Effective From
                  </label>
                  <input
                    type="date"
                    required
                    value={feeForm.effectiveFrom}
                    onChange={(e) =>
                      setFeeForm({ ...feeForm, effectiveFrom: e.target.value })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Reason for Change
                  </label>
                  <textarea
                    rows={3}
                    value={feeForm.changeReason}
                    onChange={(e) =>
                      setFeeForm({ ...feeForm, changeReason: e.target.value })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                    placeholder="Optional: Explain why fees are being updated"
                  />
                </div>
                <div className="flex gap-2 pt-4">
                  <button
                    type="submit"
                    className="flex-1 bg-indigo-600 text-white px-4 py-2 rounded-md hover:bg-indigo-700"
                  >
                    Update Fees
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowFeeModal(false)}
                    className="flex-1 bg-gray-200 text-gray-800 px-4 py-2 rounded-md hover:bg-gray-300"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* Edit Custom Fields Modal */}
      {showFieldsModal && selectedForm && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-10 mx-auto p-5 border w-[700px] shadow-lg rounded-md bg-white max-h-[90vh] overflow-y-auto">
            <div className="mt-3">
              <h3 className="text-lg font-medium leading-6 text-gray-900 mb-4">
                Custom Fields - {selectedForm.formName}
              </h3>

              {/* Existing Custom Fields */}
              <div className="mb-6">
                <h4 className="font-medium text-gray-700 mb-2">
                  Current Fields
                </h4>
                {customFields.length === 0 ? (
                  <p className="text-gray-500 text-sm">
                    No custom fields added yet.
                  </p>
                ) : (
                  <div className="space-y-2">
                    {customFields.map((field, index) => (
                      <div
                        key={index}
                        className="flex items-center justify-between bg-gray-50 p-3 rounded-lg"
                      >
                        <div>
                          <p className="font-medium">{field.label}</p>
                          <p className="text-sm text-gray-600">
                            Type: {field.fieldType} | Name: {field.fieldName} |{" "}
                            {field.isRequired ? "Required" : "Optional"}
                          </p>
                        </div>
                        <button
                          onClick={() => handleRemoveCustomField(index)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <TrashIcon className="h-5 w-5" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Add New Field Form */}
              <div className="border-t pt-4">
                <h4 className="font-medium text-gray-700 mb-3">
                  Add New Field
                </h4>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Field Name
                    </label>
                    <input
                      type="text"
                      value={newField.fieldName}
                      onChange={(e) =>
                        setNewField({ ...newField, fieldName: e.target.value })
                      }
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 text-sm"
                      placeholder="e.g., buildingHeight"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Field Label
                    </label>
                    <input
                      type="text"
                      value={newField.label}
                      onChange={(e) =>
                        setNewField({ ...newField, label: e.target.value })
                      }
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 text-sm"
                      placeholder="e.g., Building Height (m)"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Field Type
                    </label>
                    <select
                      value={newField.fieldType}
                      onChange={(e) =>
                        setNewField({ ...newField, fieldType: e.target.value })
                      }
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 text-sm"
                    >
                      <option value="text">Text</option>
                      <option value="number">Number</option>
                      <option value="email">Email</option>
                      <option value="date">Date</option>
                      <option value="select">Select/Dropdown</option>
                      <option value="textarea">Text Area</option>
                      <option value="checkbox">Checkbox</option>
                    </select>
                  </div>
                  <div className="flex items-center">
                    <label className="flex items-center">
                      <input
                        type="checkbox"
                        checked={newField.isRequired}
                        onChange={(e) =>
                          setNewField({
                            ...newField,
                            isRequired: e.target.checked,
                          })
                        }
                        className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                      />
                      <span className="ml-2 text-sm text-gray-700">
                        Required Field
                      </span>
                    </label>
                  </div>
                </div>
                <button
                  onClick={handleAddCustomField}
                  className="mt-3 flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
                >
                  <PlusIcon className="h-4 w-4" />
                  Add Field
                </button>
              </div>

              {/* Action Buttons */}
              <div className="flex gap-2 pt-6 border-t mt-6">
                <button
                  onClick={handleSaveCustomFields}
                  className="flex-1 bg-indigo-600 text-white px-4 py-2 rounded-md hover:bg-indigo-700"
                >
                  Save Custom Fields
                </button>
                <button
                  onClick={() => setShowFieldsModal(false)}
                  className="flex-1 bg-gray-200 text-gray-800 px-4 py-2 rounded-md hover:bg-gray-300"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FormManagement;
