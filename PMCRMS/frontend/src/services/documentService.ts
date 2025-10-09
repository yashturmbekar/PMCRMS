import apiClient from "./apiClient";
import type {
  ApplicationDocument,
  FileUploadResponse,
  ApiResponse,
  DocumentType,
} from "../types";

const endpoint = "/documents";

export const documentService = {
  async uploadDocument(
    applicationId: number,
    file: File,
    documentType: DocumentType
  ): Promise<ApiResponse<FileUploadResponse>> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("documentType", documentType);

    return apiClient.postWithFiles(
      `${endpoint}/upload/${applicationId}`,
      formData
    );
  },

  async getApplicationDocuments(
    applicationId: number
  ): Promise<ApiResponse<ApplicationDocument[]>> {
    return apiClient.get(`${endpoint}/application/${applicationId}`);
  },

  async getDocument(
    documentId: number
  ): Promise<ApiResponse<ApplicationDocument>> {
    return apiClient.get(`${endpoint}/${documentId}`);
  },

  async downloadDocument(documentId: number): Promise<Blob> {
    return apiClient.get(`${endpoint}/download/${documentId}`, {
      responseType: "blob",
    });
  },

  async deleteDocument(documentId: number): Promise<ApiResponse> {
    return apiClient.delete(`${endpoint}/${documentId}`);
  },

  async verifyDocument(
    documentId: number,
    isVerified: boolean,
    remarks?: string
  ): Promise<ApiResponse> {
    return apiClient.patch(`${endpoint}/${documentId}/verify`, {
      isVerified,
      remarks,
    });
  },

  async uploadMultipleDocuments(
    applicationId: number,
    files: { file: File; documentType: DocumentType }[]
  ): Promise<ApiResponse<FileUploadResponse[]>> {
    const formData = new FormData();

    files.forEach((item, index) => {
      formData.append(`files[${index}]`, item.file);
      formData.append(`documentTypes[${index}]`, item.documentType);
    });

    return apiClient.postWithFiles(
      `${endpoint}/upload-multiple/${applicationId}`,
      formData
    );
  },
};
