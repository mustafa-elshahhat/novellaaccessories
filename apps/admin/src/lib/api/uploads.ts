import { api } from "./client";
import type { UploadedImageDto } from "./types";

export const uploadsApi = {
  image: (file: File, entityType?: string, entityId?: string) => {
    const form = new FormData();
    form.set("file", file);
    if (entityType) form.set("entityType", entityType);
    if (entityId) form.set("entityId", entityId);
    return api.form<UploadedImageDto>("/api/admin/uploads/image", form);
  }
};
