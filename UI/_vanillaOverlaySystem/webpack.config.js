const TerserPlugin = require('terser-webpack-plugin');
const { merge } = require("webpack-merge");
const singleSpaDefaults = require("webpack-config-single-spa-ts");
const CopyPlugin = require("copy-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = (webpackConfigEnv, argv) => {
  const orgName = "k45-euis";
  const defaultConfig = singleSpaDefaults({
    orgName,
    projectName: "vos-config",
    webpackConfigEnv,
    argv,
    disableHtmlGeneration: true,
  });

  return [merge(defaultConfig, {
    // modify the webpack config however you'd like to by adding to this object
    plugins: [
      new MiniCssExtractPlugin({
        filename: `base.css`
      }),
      new CopyPlugin({
        patterns: [
          "dependencies/*",
          "*.html"
        ],
      }),
    ],
    module: {
      rules: [
        {
          test: /\.(s[ac]|c)ss$/i,
          use: [
            MiniCssExtractPlugin.loader,
            {
              loader: "css-loader",
            },
            "sass-loader",
          ],
        },

      ],
    },
  }),{
    entry: {
        "vos-loader": "./vos-loader.js"
    },
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin()]
    },
    mode: "development"
}];
};
