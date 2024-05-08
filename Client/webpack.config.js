var path = require("path");

module.exports = {
    mode: "development",
    entry: "./src/App.fs.js",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        static: "./public",
        port: 8080,
        proxy: {
            '/IAlchemyApi/**': {
                target: "http://localhost:5000/Alchemy",   // backend server is running on port 5000 during development
                changeOrigin: true
            }
        }
    },
    module: {
    }
}
