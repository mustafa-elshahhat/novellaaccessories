import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";
import { fileURLToPath, URL } from "node:url";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "VITE_");
  const apiTarget = env.VITE_API_BASE_URL || "http://localhost:5000";
  return {
    plugins: [react()],
    resolve: {
      alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) }
    },
    server: {
      port: 5173,
      proxy: apiTarget ? { "/api": { target: apiTarget, changeOrigin: true } } : undefined
    },
    build: {
      outDir: "dist",
      sourcemap: false,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes("node_modules/react") || id.includes("node_modules/react-dom") || id.includes("node_modules/react-router-dom")) return "react";
            if (id.includes("@tanstack/react-query")) return "query";
            if (id.includes("react-hook-form") || id.includes("zod") || id.includes("@hookform/resolvers")) return "forms";
            return undefined;
          }
        }
      }
    }
  };
});
