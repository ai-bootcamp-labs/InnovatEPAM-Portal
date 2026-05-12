module.exports = {
  root: true,
  env: { browser: true, es2022: true, node: true },
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 2022,
    sourceType: 'module',
    ecmaFeatures: { jsx: true },
  },
  settings: { react: { version: '18.3' } },
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react/recommended',
    'plugin:react/jsx-runtime',
    'plugin:react-hooks/recommended',
    'plugin:jsx-a11y/recommended',
  ],
  plugins: ['@typescript-eslint', 'react', 'react-hooks', 'jsx-a11y'],
  ignorePatterns: ['dist', 'node_modules', 'playwright-report', '.test-results'],
  rules: {
    '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
    'react/prop-types': 'off',
  },
  overrides: [
    {
      files: ['src/**/*.{ts,tsx}'],
      excludedFiles: ['src/lib/date.ts'],
      rules: {
        // T110 — formatting must go through src/lib/date.ts so locale + tokens
        // stay consistent with the spec (formatIdeaDate / formatIdeaDateTime /
        // formatRelative). Direct Intl/Date string conversions bypass the
        // helper and produce locale-dependent output.
        'no-restricted-syntax': [
          'error',
          {
            selector: "CallExpression[callee.property.name='toLocaleDateString']",
            message: 'Use formatIdeaDate / formatIdeaDateTime from @/lib/date instead of toLocaleDateString.',
          },
          {
            selector: "CallExpression[callee.property.name='toLocaleString']",
            message: 'Use formatIdeaDateTime from @/lib/date instead of toLocaleString.',
          },
          {
            selector: "CallExpression[callee.property.name='toLocaleTimeString']",
            message: 'Use formatIdeaDateTime from @/lib/date instead of toLocaleTimeString.',
          },
        ],
      },
    },
    {
      files: ['tests/**/*.{ts,tsx}'],
      rules: {
        '@typescript-eslint/no-explicit-any': 'off',
      },
    },
  ],
};
