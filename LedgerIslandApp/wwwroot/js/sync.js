export async function startSync() {
    const get = name => document.cookie.split('; ').find(r => r.startsWith(name + '='))?.split('=')[1];
    const sid = get('sid_id');
    const ctl = get('ctl');
    if (!sid || !ctl) return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/sync?sid_id=${sid}&ctl=${ctl}`)
        .withAutomaticReconnect()
        .build();

    connection.on("ForceLogout", async () => {
        // ask Blazor to show the banner
        if (window.DotNet) {
            await DotNet.invokeMethodAsync(
                "LedgerIslandApp",   // your assembly name
                "ShowLogoutMessage",
                "You have been logged out because you signed in elsewhere."
            );
        }

        // wait 2 seconds, then clear session + redirect
        setTimeout(() => {
            document.cookie = "sid=; Max-Age=0; path=/";
            window.location.href = "/";
        }, 2000);
    });

    await connection.start().catch(() => { });
}
