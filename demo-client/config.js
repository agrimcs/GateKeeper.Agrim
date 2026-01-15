// OAuth Configuration
// Update these values based on your GateKeeper setup
const CONFIG = {
    CLIENT_ID: 'chatgpt',  // Update this after registering in GateKeeper
    REDIRECT_URI: 'http://localhost:8080/callback.html',
    AUTHORITY: 'https://localhost:44330', // gatekeeper app
    SCOPES: 'openid profile email offline_access'
};
