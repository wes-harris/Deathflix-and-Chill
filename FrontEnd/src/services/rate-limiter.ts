export class RateLimiter {
    private tokens: number;
    private lastRefill: number;
    
    constructor(
        private readonly maxTokens: number,
        private readonly refillRate: number, // tokens per second
        private readonly refillInterval: number = 1000 // milliseconds
    ) {
        this.tokens = maxTokens;
        this.lastRefill = Date.now();
    }

    async acquireToken(): Promise<void> {
        await this.refillTokens();
        
        if (this.tokens <= 0) {
            const waitTime = (1 / this.refillRate) * 1000;
            await new Promise(resolve => setTimeout(resolve, waitTime));
            await this.refillTokens();
        }
        
        this.tokens--;
    }

    private async refillTokens(): Promise<void> {
        const now = Date.now();
        const timePassed = now - this.lastRefill;
        const refillAmount = (timePassed / this.refillInterval) * this.refillRate;
        
        this.tokens = Math.min(this.maxTokens, this.tokens + refillAmount);
        this.lastRefill = now;
    }
}