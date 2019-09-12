import React from "react";
import "./App.css";
import { UserAgentApplication } from "msal";
import { message, Button } from "antd";
import "antd/dist/antd.css";

const tenantId = "39c9840c-debc-4da2-80a5-ebc2218d127b";
const instance = "https://login.microsoftonline.com/";
const appId = "f478b937-a6e4-41a0-9159-e34b80943011";
const authority = `${instance}/${tenantId}`;

const aadClient = new UserAgentApplication({
  auth: {
    authority,
    clientId: appId,
    redirectUri: "http://localhost:5000",
  }
});
aadClient.handleRedirectCallback(
  (authError, authResponse) => {
    console.log("token received callback");
    console.log({ authError, authResponse });
  },
  (authError, accountState) => {
    console.error("error received callback");
    console.log({ authError, accountState });
  }
);
aadClient.handleRedirectCallback = (v1, v2, v3) => {
  console.log("handle redirect callback", { v1, v2, v3 });
};

const consentScopes = ["Calendars.Read","Group.Read.All","User.Read","Files.Read.All"]

const scopes = [
  "api://f478b937-a6e4-41a0-9159-e34b80943011/user_impersonation",
  // "f478b937-a6e4-41a0-9159-e34b80943011/test.scope",
  // "f478b937-a6e4-41a0-9159-e34b80943011/test.scope2"
];

function App() {
  React.useEffect(() => {
    console.log("account:", aadClient.getAccount());
  }, []);
  return (
    <div className="App">
      <header className="App-header">
        <Button
          onClick={() =>
            aadClient.loginRedirect({ prompt: "select_account", scopes: consentScopes })
          }
        >
          Login
        </Button>
        <Button
          onClick={() =>
            aadClient.loginRedirect({ prompt: "consent", scopes: consentScopes })
          }
        >
          Consent {JSON.stringify(consentScopes)}
        </Button>
        <Button
          onClick={async () => {
            const token = await aadClient.acquireTokenSilent({
              scopes
            });
            console.log("access token", token.accessToken);
          }}
        >
          Acquire token silent
        </Button>

        <Button
          onClick={async () => {
            try {
              const token = await aadClient.acquireTokenSilent({
                scopes,
              });
              console.log("invoke api with token", token.accessToken)
              const response = await fetch("/api/Profile", {
                headers: {
                  Authorization: "Bearer " + token.accessToken
                }
              });

              console.log("response", response);

              const text = await response.text();
              console.log("text", text);
              if (!response.ok) {
                message.error(response.statusText + " " + text);
              } else {
                message.info(text);
              }
            } catch (E) {
              message.error(E.toString());
            }
          }}
        >
          Invoke backend
        </Button>
        <Button onClick={()=>aadClient.clearCache()}>Clear token cache</Button>
        <Button
          onClick={async () => {
            try {
              const token = await aadClient.acquireTokenSilent({
                scopes,
              });
              console.log("invoke api with token", token.accessToken)
              const response = await fetch("/api/Profile/profile", {
                headers: {
                  Authorization: "Bearer " + token.accessToken
                }
              });

              console.log("response", response);

              const text = await response.text();
              console.log("text", text);
              if (!response.ok) {
                message.error(response.statusText + " " + text);
              } else {
                message.info(text);
              }
            } catch (E) {
              message.error(E.toString());
            }
          }}
        >
          Get Profile with OBO
        </Button>
      </header>
    </div>
  );
}

export default App;
