interface EuisMainModel {
    currentTime: string,
    currentDate: string,
    monitorIdentifierText: string,
    monitorNumber: number
}

declare var __euis_main: EuisMainModel
declare var __euisReady: Promise<boolean>

type ApplicationInjectionData = {
    appName: string,
    displayName: string,
    jsUrl: string,
    iconUrl: string
}

interface RootProps {
    getAppData(): Record<string, ApplicationInjectionData & { button?: HTMLDivElement }>
    getAppNames(): Promise<string[]>
    getQuantityMonitors(): Promise<number>
    listActiveAppsInMonitor(monitorId: number): Promise<string[]>
    readonly name: string
    reintroduceAppButton(appName: string, monitorId: number): void
    removeAppButton(appName: string, monitorId: number): void
    singleSpa: typeof import('single-spa')
}