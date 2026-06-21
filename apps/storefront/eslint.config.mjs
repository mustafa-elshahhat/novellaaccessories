import next from "eslint-config-next";

// Next.js 16 ships `eslint-config-next` as a native flat-config array, so it is spread directly
// (no FlatCompat — passing a flat config through FlatCompat.extends triggers an eslintrc
// circular-structure validation error).
const eslintConfig = [
  {
    ignores: [".next/**", "node_modules/**", "coverage/**", "next-env.d.ts"],
  },
  ...next,
];

export default eslintConfig;
