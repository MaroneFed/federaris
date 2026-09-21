using UnityEngine;

namespace Fief
{
    /// <summary>Resultat d'une transaction. Le message est directement affichable a l'ecran.</summary>
    public struct TradeResult
    {
        public bool success;
        public int quantity;
        public int gold;
        public string message;

        public static TradeResult Fail(string reason)
        {
            TradeResult r = new TradeResult();
            r.success = false;
            r.message = reason;
            return r;
        }
    }

    /// <summary>
    /// Le marche central et ses prix dynamiques.
    ///
    /// LE PRINCIPE : chaque ressource a un stock. Prix = prix_de_base * (equilibre / stock) ^ elasticite.
    ///   - Tu vends 40 bois d'un coup  -> le stock monte -> le prix s'effondre.
    ///   - Personne ne vend de fer     -> le stock fond   -> le fer devient tres cher.
    /// Et le prix de chaque unite est recalcule apres chaque unite vendue : ecouler
    /// une grosse cargaison rapporte donc moins que deux petites, espacees dans le temps.
    /// C'est ce qui rendra le marche interessant a 6 joueurs.
    ///
    /// AUTORITE SERVEUR (decision verrouillee du brief) : tout passe par RequestSell /
    /// RequestBuy. Aucun autre code ne touche l'or, le stock ou l'inventaire.
    /// En Phase 3 ces deux methodes deviennent des ServerRpc executees chez l'hote,
    /// et le reste du jeu n'aura pas une ligne a changer.
    /// </summary>
    public class Market
    {
        class Line
        {
            public float basePrice;
            public float equilibrium;
            public float stock;
        }

        readonly Line[] lines = new Line[ResourceInfo.Count];

        float elasticity = 0.62f;
        float buySpread = 1.18f;
        float driftPerSecond = 0.35f;

        const float MinPriceFactor = 0.38f;
        const float MaxPriceFactor = 2.80f;

        public Market(GameConfig cfg)
        {
            if (cfg != null)
            {
                elasticity = cfg.priceElasticity;
                buySpread = cfg.buySpread;
                driftPerSecond = cfg.marketDriftPerSecond;
            }

            // Prix de base cales sur la Porte 1 : ~20 min pour financer les 5 constructions
            // (2380 or) sur un monde de 484 hectares. Valeur par kilo croissante :
            // Bois 6.2 or/kg, Pierre 7.0, Fer 7.8 -> le lourd paie mieux, mais ralentit plus.
            lines[(int)ResourceType.Wood] = NewLine(6.2f, 130f);
            lines[(int)ResourceType.Stone] = NewLine(14.0f, 95f);
            lines[(int)ResourceType.Iron] = NewLine(23.4f, 60f);
        }

        static Line NewLine(float basePrice, float equilibrium)
        {
            Line l = new Line();
            l.basePrice = basePrice;
            l.equilibrium = equilibrium;
            l.stock = equilibrium;
            return l;
        }

        public float Stock(ResourceType type) { return lines[(int)type].stock; }
        public float BasePrice(ResourceType type) { return lines[(int)type].basePrice; }
        public float Equilibrium(ResourceType type) { return lines[(int)type].equilibrium; }

        /// <summary>Prix auquel le marche te RACHETE une unite, au stock actuel.</summary>
        public float SellPrice(ResourceType type)
        {
            return UnitPrice(lines[(int)type], lines[(int)type].stock);
        }

        /// <summary>Prix auquel le marche te VEND une unite (marge du marchand incluse).</summary>
        public float BuyPrice(ResourceType type)
        {
            return UnitPrice(lines[(int)type], lines[(int)type].stock) * buySpread;
        }

        /// <summary>De -1 (prix au plancher) a +1 (prix au plafond). Sert a la fleche de tendance du HUD.</summary>
        public float PriceTrend(ResourceType type)
        {
            Line l = lines[(int)type];
            float ratio = UnitPrice(l, l.stock) / l.basePrice;
            return Mathf.Clamp((ratio - 1f) / 0.8f, -1f, 1f);
        }

        float UnitPrice(Line line, float stock)
        {
            float safeStock = Mathf.Max(1f, stock);
            float price = line.basePrice * Mathf.Pow(line.equilibrium / safeStock, elasticity);
            return Mathf.Clamp(price,
                               line.basePrice * MinPriceFactor,
                               line.basePrice * MaxPriceFactor);
        }

        /// <summary>Les stocks reviennent lentement vers l'equilibre : les prix se remettent d'un krach.</summary>
        public void Tick(float deltaTime)
        {
            float step = driftPerSecond * deltaTime;
            for (int i = 0; i < lines.Length; i++)
            {
                Line l = lines[i];
                l.stock = Mathf.MoveTowards(l.stock, l.equilibrium, step);
            }
        }

        /// <summary>Simule une vente sans rien modifier : sert a afficher "tu toucherais X".</summary>
        public int QuoteSell(ResourceType type, int quantity)
        {
            Line l = lines[(int)type];
            float stock = l.stock;
            float total = 0f;
            for (int i = 0; i < quantity; i++)
            {
                total += UnitPrice(l, stock);
                stock += 1f;
            }
            return Mathf.RoundToInt(total);
        }

        public int QuoteBuy(ResourceType type, int quantity)
        {
            Line l = lines[(int)type];
            float stock = l.stock;
            float total = 0f;
            for (int i = 0; i < quantity; i++)
            {
                total += UnitPrice(l, stock) * buySpread;
                stock = Mathf.Max(1f, stock - 1f);
            }
            return Mathf.RoundToInt(total);
        }

        // ------------------------------------------------------------------
        //  POINT D'ENTREE AUTORITAIRE
        // ------------------------------------------------------------------

        public TradeResult RequestSell(Inventory inventory, Wallet wallet, ResourceType type, int quantity)
        {
            if (inventory == null || wallet == null) return TradeResult.Fail("Systeme indisponible");
            if (quantity <= 0) return TradeResult.Fail("Quantite invalide");

            int available = inventory.Get(type);
            if (available <= 0) return TradeResult.Fail("Tu n'as pas de " + ResourceInfo.Name(type));

            int sold = Mathf.Min(quantity, available);

            Line l = lines[(int)type];
            float total = 0f;
            for (int i = 0; i < sold; i++)
            {
                total += UnitPrice(l, l.stock);
                l.stock += 1f;
            }

            int gold = Mathf.RoundToInt(total);
            inventory.TryRemove(type, sold);
            wallet.Add(gold);

            TradeResult result = new TradeResult();
            result.success = true;
            result.quantity = sold;
            result.gold = gold;
            result.message = "Vendu " + sold + " " + ResourceInfo.Name(type) + " pour " + gold + " or";
            return result;
        }

        public TradeResult RequestBuy(Inventory inventory, Wallet wallet, ResourceType type, int quantity)
        {
            if (inventory == null || wallet == null) return TradeResult.Fail("Systeme indisponible");
            if (quantity <= 0) return TradeResult.Fail("Quantite invalide");

            Line l = lines[(int)type];
            float stock = l.stock;
            float total = 0f;
            int bought = 0;

            for (int i = 0; i < quantity; i++)
            {
                if (inventory.SpaceFor(type) - bought <= 0) break;
                float unit = UnitPrice(l, stock) * buySpread;
                if (Mathf.RoundToInt(total + unit) > wallet.Gold) break;
                total += unit;
                stock = Mathf.Max(1f, stock - 1f);
                bought++;
            }

            if (bought <= 0)
            {
                if (inventory.SpaceFor(type) <= 0) return TradeResult.Fail("Sac plein");
                return TradeResult.Fail("Pas assez d'or");
            }

            int cost = Mathf.RoundToInt(total);
            if (!wallet.TrySpend(cost)) return TradeResult.Fail("Pas assez d'or");

            l.stock = stock;
            inventory.TryAdd(type, bought);

            TradeResult result = new TradeResult();
            result.success = true;
            result.quantity = bought;
            result.gold = -cost;
            result.message = "Achete " + bought + " " + ResourceInfo.Name(type) + " pour " + cost + " or";
            return result;
        }
    }
}
