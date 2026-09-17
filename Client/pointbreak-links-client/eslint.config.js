// @ts-check
const eslint = require("@eslint/js");
const { defineConfig } = require("eslint/config");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");

module.exports = defineConfig([
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      "@angular-eslint/directive-selector": [
        "error",
        {
          type: "attribute",
          prefix: "app",
          style: "camelCase",
        },
      ],
      "@angular-eslint/component-selector": [
        "error",
        {
          type: "element",
          prefix: "app",
          style: "kebab-case",
        },
      ],
      // Every modal in this app exposes a `close = output<void>()` — a deliberate, consistent
      // naming convention across ~10 components, not an accidental collision with the native
      // DOM `close` event (nothing here extends <dialog> or relies on native `close` bubbling).
      // Renaming it now would mean touching every modal component plus every parent template's
      // `(close)="..."` binding for a purely stylistic rule; not worth the blast radius.
      "@angular-eslint/no-output-native": "off",
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      angular.configs.templateRecommended,
      angular.configs.templateAccessibility,
    ],
    rules: {
      // Every modal's backdrop (`.modal-overlay`, `(click)="close.emit()"`) is a click-to-dismiss
      // convenience, not the only way to close — every modal also has an explicit, focusable,
      // aria-labelled "×" button that already satisfies keyboard/screen-reader access. Making the
      // backdrop itself focusable (tabindex + keydown handlers) would add a meaningless stop in
      // the tab order rather than fix anything real.
      "@angular-eslint/template/click-events-have-key-events": "off",
      "@angular-eslint/template/interactive-supports-focus": "off",
    },
  }
]);
