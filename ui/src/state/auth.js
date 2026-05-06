import { createContext, useContext, useMemo, useState } from "react";
import { authApi } from "../api/auth.js";

const AuthContext = createContext(null);
const STORAGE_KEY = "easymitt.session";

function readSession() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(readSession);
  const [languageOverride, setLanguageOverride] = useState(null);
  const user = session?.user || null;
  const language = languageOverride || user?.language || "tr";

  const value = useMemo(
    () => ({
      session,
      user,
      language,
      canWrite: user?.role === "Admin" || user?.role === "Accountant",
      async login(email, password) {
        const next = await authApi.login(email, password);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        setSession(next);
        setLanguageOverride(null);
      },
      logout() {
        localStorage.removeItem(STORAGE_KEY);
        setSession(null);
        setLanguageOverride(null);
      },
      setLanguage(nextLanguage) {
        setLanguageOverride(nextLanguage);
        if (session) {
          const next = { ...session, user: { ...session.user, language: nextLanguage } };
          localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
          setSession(next);
        }
      }
    }),
    [language, session, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used inside AuthProvider");
  return value;
}
