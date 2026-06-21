import { api } from "./client";
import type { Success, UploadedImageDto } from "./types";

export const uploadsApi = {
  image: (file: File, entityType?: string, entityId?: string) => {
    const form = new FormData();
    form.set("file", file);
    if (entityType) form.set("entityType", entityType);
    if (entityId) form.set("entityId", entityId);
    return api.form<UploadedImageDto>("/api/admin/uploads/image", form);
  },
  deleteImage: (publicId: string) => api.delete<Success>("/api/admin/uploads/image", { publicId })
};
