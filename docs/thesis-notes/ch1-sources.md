# Chapter 1 sources: annotated literature scan (P0-T3)

20 sources in five groups. Each entry: full reference, one paragraph on what it says, and how this thesis uses it. Status column: **verified** means title, authors, venue and year were checked online on 2026-11-14; **check** means one detail (noted) must be confirmed against the publisher page before it goes into the bibliography.

Reference style is a placeholder (APA-like); convert to the GTU template in P8-T5.

| # | Short | Group | Status |
|---|---|---|---|
| 1 | Lewis et al. 2020 | RAG | verified |
| 2 | Gao et al. 2023 | RAG | verified |
| 3 | Karpukhin et al. 2020 | RAG | verified |
| 4 | Liu et al. 2024 | RAG | verified |
| 5 | Es et al. 2024 | RAG evaluation | verified |
| 6 | Zheng et al. 2023 | RAG evaluation | verified |
| 7 | Chen et al. 2024 (BGE M3) | Multilingual embeddings | check venue |
| 8 | Wang et al. 2024 (mE5) | Multilingual embeddings | verified |
| 9 | Enevoldsen et al. 2025 (MMTEB) | Multilingual embeddings | verified |
| 10 | GeorgianFAQRetrieval / Georgian tokenization | Georgian | check authors |
| 11 | Bloom 1984 | Tutoring | verified |
| 12 | VanLehn 2011 | Tutoring | verified |
| 13 | Graesser et al. 2004 (AutoTutor) | Socratic tutoring | verified |
| 14 | Al-Hossami et al. 2024 | Socratic tutoring with LLMs | verified |
| 15 | Kasneci et al. 2023 | LLMs in education | verified |
| 16 | Bastani et al. 2025 | LLMs in education | verified |
| 17 | Kestin et al. 2025 | AI tutors | verified |
| 18 | Liffiton et al. 2023 (CodeHelp) and Kazemitabaar et al. 2024 (CodeAid) | Guardrailed AI tutors | verified |
| 19 | OWASP 2026; Greshake et al. 2023; Perez and Ribeiro 2022 | LLM security | verified |
| 20 | Brooke 1996; Bangor et al. 2009 | Usability | verified |

## A. Retrieval-augmented generation

**1. Lewis, P., Perez, E., Piktus, A., Petroni, F., Karpukhin, V., Goyal, N., Küttler, H., Lewis, M., Yih, W., Rocktäschel, T., Riedel, S., & Kiela, D. (2020). Retrieval-augmented generation for knowledge-intensive NLP tasks. *Advances in Neural Information Processing Systems 33 (NeurIPS 2020)*. arXiv:2005.11401.**
The paper that named RAG. A pretrained sequence-to-sequence generator is combined with a dense retriever over a Wikipedia index; retrieved passages are treated as a latent variable and the model is trained end to end. RAG beat parametric-only models on open-domain QA and produced more specific, factual text, and the index can be swapped without retraining. *Use:* definition and origin of RAG in chapter 1; justification that knowledge lives in the index (the course materials), so updating a course needs re-indexing, not retraining.

**2. Gao, Y., Xiong, Y., Gao, X., Jia, K., Pan, J., Bi, Y., Dai, Y., Sun, J., Wang, M., & Wang, H. (2023). Retrieval-augmented generation for large language models: A survey. arXiv:2312.10997.**
Survey that sorts RAG systems into naive, advanced and modular RAG, and reviews retrieval (chunking, embedding choice, query rewriting), augmentation and generation stages, plus evaluation. *Use:* taxonomy for positioning this project as "advanced RAG" (query condensing, similarity threshold, citation validation) and for the related-work table in chapter 1.

**3. Karpukhin, V., Oğuz, B., Min, S., Lewis, P., Wu, L., Edunov, S., Chen, D., & Yih, W. (2020). Dense passage retrieval for open-domain question answering. *Proceedings of EMNLP 2020*, 6769–6781.**
Shows that dense dual-encoder embeddings trained on question–passage pairs outperform BM25 for passage retrieval by a large margin in top-k accuracy. *Use:* background on why dense vector search (cosine distance on embeddings) is used instead of keyword search, and the source of the top-k / Recall@k evaluation style used in experiment 1.

**4. Liu, N. F., Lin, K., Hewitt, J., Paranjape, A., Bevilacqua, M., Petroni, F., & Liang, P. (2024). Lost in the middle: How language models use long contexts. *Transactions of the Association for Computational Linguistics, 12*, 157–173.**
Models use information best when it is at the beginning or end of the context and noticeably worse when it sits in the middle, even for long-context models. *Use:* argument for a small top-k with the most similar chunks first, instead of filling the context window; supports the choice to number and order context blocks.

**5. Es, S., James, J., Espinosa-Anke, L., & Schockaert, S. (2024). RAGAS: Automated evaluation of retrieval augmented generation. *Proceedings of EACL 2024: System Demonstrations*, 150–158.**
Reference-free metrics for RAG computed by an LLM: faithfulness (is the answer supported by the context), answer relevance and context relevance. *Use:* conceptual basis for the Groundedness, Relevance and Retrieval evaluators of Microsoft.Extensions.AI.Evaluation used in experiment 2; discussion of evaluating without a full gold answer set.

**6. Zheng, L., Chiang, W.-L., Sheng, Y., Zhuang, S., Wu, Z., Zhuang, Y., Lin, Z., Li, Z., Li, D., Xing, E. P., Zhang, H., Gonzalez, J. E., & Stoica, I. (2023). Judging LLM-as-a-judge with MT-Bench and Chatbot Arena. *Advances in Neural Information Processing Systems 36 (NeurIPS 2023), Datasets and Benchmarks Track*.**
Strong LLM judges agree with human preferences at roughly the level humans agree with each other, but show position, verbosity and self-enhancement biases. *Use:* justification and limitation of LLM-as-judge in experiments 2 and 3; reason for hand-scoring 10 answers and manually reviewing every jailbreak response (risk table, PLAN section 9).

## B. Multilingual and Georgian embeddings

**7. Chen, J., Xiao, S., Zhang, P., Luo, K., Lian, D., & Liu, Z. (2024). BGE M3-Embedding: Multi-lingual, multi-functionality, multi-granularity text embeddings through self-knowledge distillation. arXiv:2402.03216.** *Check:* confirm the Findings of ACL 2024 venue and page numbers in the ACL Anthology.
One model that supports 100+ languages, inputs up to 8,192 tokens, and dense, sparse and multi-vector retrieval, trained with self-knowledge distillation across the three scoring modes. State of the art on the multilingual MIRACL and MKQA benchmarks at release. *Use:* the open-source, locally runnable embedding arm of experiment 1 (via Ollama); its native 1024 dimensions match decision D3.

**8. Wang, L., Yang, N., Huang, X., Yang, L., Majumder, R., & Wei, F. (2024). Multilingual E5 text embeddings: A technical report. arXiv:2402.05672.**
Training recipe and results for the multilingual E5 family (contrastive pre-training on about a billion multilingual text pairs, then supervised fine-tuning), including an instruction-tuned large model. *Use:* background on how multilingual embedding models are built; mE5 is a candidate baseline if a fourth embedding model is needed.

**9. Enevoldsen, K., et al. (2025). MMTEB: Massive multilingual text embedding benchmark. *International Conference on Learning Representations (ICLR 2025)*. arXiv:2502.13595.**
Community extension of MTEB to 500+ tasks in 250+ languages across 10 task types. Finds that the best publicly available model across languages is the comparatively small multilingual-e5-large-instruct, not the largest LLM-based embedders. *Use:* evidence that embedding quality varies strongly by language and that public leaderboards say little about Georgian specifically, which motivates measuring it in experiment 1.

**10. Georgian-specific resources.** (a) *GeorgianFAQRetrieval*, a Georgian retrieval dataset in the MTEB collection built from FAQ question–answer pairs mined from Georgian websites (Hugging Face: `mteb/GeorgianFAQRetrieval`). (b) A comparison of WordPiece, BPE, SentencePiece and token-free tokenization for Georgian as a morphologically complex low-resource language, *Proceedings of ICNLSP 2024* (ACL Anthology `2024.icnlsp-1.22`). *Check:* authors and title of (b) and the dataset citation of (a) from their pages.
*Use:* the only Georgian retrieval benchmark found so far, and evidence for the risk that Georgian splits into many tokens per word (PLAN risk table), which is why chunk sizes are reported in tokens and words. Scan for more Georgian RAG work in the ACL Anthology before P8-T1; none was found that evaluates RAG on Georgian course material, which supports the novelty claim.

## C. Tutoring, Socratic dialogue and AI tutors

**11. Bloom, B. S. (1984). The 2 sigma problem: The search for methods of group instruction as effective as one-to-one tutoring. *Educational Researcher, 13*(6), 4–16.**
Students tutored one-to-one performed about two standard deviations better than students in conventional classes. *Use:* classic motivation for scalable individual tutoring in the introduction; to be presented with source 12 as a correction, since the 2-sigma figure is an upper bound.

**12. VanLehn, K. (2011). The relative effectiveness of human tutoring, intelligent tutoring systems, and other tutoring systems. *Educational Psychologist, 46*(4), 197–221.**
Meta-review finding human tutoring at about d = 0.79 and step-based intelligent tutoring systems at about d = 0.76, far closer than the 2-sigma claim; the gain comes from interaction granularity, not from human tutors as such. *Use:* argument that a fine-grained, interactive system (hint ladder, guiding questions) can approach human tutoring; frames the hint-ladder design.

**13. Graesser, A. C., Lu, S., Jackson, G. T., Mitchell, H. H., Ventura, M., Olney, A., & Louwerse, M. M. (2004). AutoTutor: A tutor with dialogue in natural language. *Behavior Research Methods, Instruments, & Computers, 36*(2), 180–192.**
A pre-LLM intelligent tutor that holds a mixed-initiative dialogue, giving pumps, hints, prompts and assertions in a ladder before revealing an answer, with learning gains over reading textbooks. *Use:* the direct ancestor of the hint ladder (concept reminder, pointer, guiding question); shows the Socratic escalation pattern predates LLMs.

**14. Al-Hossami, E., Bunescu, R., Smith, J., & Teehan, R. (2024). Can language models employ the Socratic method? Experiments with code debugging. *Proceedings of the 55th ACM Technical Symposium on Computer Science Education (SIGCSE 2024)*, Vol. 1, 53–59.**
A hand-built dataset of multi-turn Socratic dialogues for novice debugging, used to benchmark models from fine-tuned Flan-T5 to zero-shot and chain-of-thought GPT-4; GPT-4 is much better but still not reliably Socratic. *Use:* evidence that LLMs can be prompted into Socratic behavior but drift toward giving answers, which justifies enforcing the policy in code (D4, D6) and an output guard rather than trusting the prompt.

**15. Kasneci, E., Sessler, K., Küchemann, S., Bannert, M., Dementieva, D., Fischer, F., Gasser, U., Groh, G., Günnemann, S., Hüllermeier, E., Krusche, S., Kutyniok, G., Michaeli, T., Nerdel, C., Pfeffer, J., Poquet, O., Sailer, M., Schmidt, A., Seidel, T., Stadler, M., Weller, J., Kuhn, J., & Kasneci, G. (2023). ChatGPT for good? On opportunities and challenges of large language models for education. *Learning and Individual Differences, 103*, 102274.**
Position paper listing opportunities (personalized explanation, feedback, support for lecturers) and risks (over-reliance, bias, hallucination, privacy, copyright) of LLMs in education. *Use:* the problem statement in the introduction; each risk maps to a feature (grounding and citations for hallucination, help policy for over-reliance, pseudonyms for privacy).

**16. Bastani, H., Bastani, O., Sungu, A., Ge, H., Kabakcı, Ö., & Mariman, R. (2025). Generative AI without guardrails can harm learning: Evidence from high school mathematics. *Proceedings of the National Academy of Sciences, 122*(26), e2422633122.**
Field experiment with nearly a thousand high-school students in Turkey. Unrestricted GPT-4 improved practice performance but, once access was removed, students did worse on exams (about 17% lower) than students who never had it; a tutor version with learning-oriented prompts largely removed the harm. *Use:* the strongest empirical argument for the core thesis idea: an AI mentor must restrict help on assessed work. Limitations (one school, one subject, few sessions) are noted.

**17. Kestin, G., Miller, K., Klales, A., Milbourne, T., & Ponti, G. (2025). AI tutoring outperforms in-class active learning: An RCT introducing a novel research-based design in an authentic educational setting. *Scientific Reports, 15*, 17458. https://doi.org/10.1038/s41598-025-97652-6** *Check:* co-author list and article number against the DOI page.
Randomized crossover study in an introductory physics course: a carefully prompted GPT-4 tutor that followed pedagogical best practice produced larger learning gains in less time than an active-learning class, with higher reported engagement. *Use:* evidence that a pedagogically designed tutor, not a generic chatbot, is what helps; supports the hint-ladder and grounding design. Counterweight to source 16.

**18. Guardrailed course assistants.** (a) Liffiton, M., Sheese, B., Savelka, J., & Denny, P. (2023). CodeHelp: Using large language models with guardrails for scalable support in programming classes. *Proceedings of the 23rd Koli Calling International Conference on Computing Education Research*, 1–11. https://doi.org/10.1145/3631802.3631830. (b) Kazemitabaar, M., Ye, R., Wang, X., Henley, A. Z., Denny, P., Craig, M., & Grossman, T. (2024). CodeAid: Evaluating a classroom deployment of an LLM-based programming assistant that balances student and educator needs. *Proceedings of CHI 2024*. arXiv:2401.11314.
Two deployed systems that help programming students without handing out solutions: CodeHelp through prompt-level guardrails and a sufficiency check, CodeAid through answer types (conceptual answers, pseudo-code, annotated fixes) studied over a semester with 8,000+ queries and instructor interviews. Both report heavy use outside staff hours and student attempts to extract solutions. *Use:* closest related work. Differences this thesis claims: answers grounded in the lecturer's own materials with page citations, a per-assignment policy set by the lecturer, applied in both modes (D5), and a measured jailbreak experiment.

## D. Security of LLM applications

**19. LLM security.** (a) OWASP GenAI Security Project. (2026). *OWASP Top 10 for LLM Applications 2026*. https://genai.owasp.org/resource/owasp-genai-llm-top-10-2026/ (released August 2026; the 2025 edition from November 2024 is the predecessor). (b) Greshake, K., Abdelnabi, S., Mishra, S., Endres, C., Holz, T., & Fritz, M. (2023). Not what you've signed up for: Compromising real-world LLM-integrated applications with indirect prompt injection. *Proceedings of the 16th ACM Workshop on Artificial Intelligence and Security (AISec '23)*, 79–90. (c) Perez, F., & Ribeiro, I. (2022). Ignore previous prompt: Attack techniques for language models. *NeurIPS 2022 ML Safety Workshop*. arXiv:2211.09527.
OWASP lists prompt injection as the top risk, followed by sensitive information disclosure; the 2026 edition reorders most entries and promotes excessive agency. Greshake et al. show that instructions planted in retrieved content (web pages, documents) can take over an LLM-integrated application, which is exactly the RAG threat of a poisoned lecture file. Perez and Ribeiro show goal hijacking and prompt leaking with simple "ignore previous instructions" inputs. *Use:* threat model and the OWASP mapping in `docs/security.md` (P4-T8) must use the **2026** numbering; direct-injection attack types in the test set (P6-T3); indirect-injection defense and poisoned-document test (P4-T6). *Note:* PLAN.md says "OWASP Top 10 for LLM Applications" without a year; use 2026.

## E. Usability measurement

**20. Usability.** (a) Brooke, J. (1996). SUS: A "quick and dirty" usability scale. In P. W. Jordan, B. Thomas, B. A. Weerdmeester, & I. L. McClelland (Eds.), *Usability Evaluation in Industry* (pp. 189–194). Taylor & Francis. (b) Bangor, A., Kortum, P., & Miller, J. (2009). Determining what individual SUS scores mean: Adding an adjective rating scale. *Journal of Usability Studies, 4*(3), 114–123.
Brooke defines the 10-item SUS and its 0–100 scoring; Bangor et al. map scores to adjectives and acceptability ranges (about 68 as average, above about 70 acceptable). *Use:* instrument and interpretation for experiment 4 (NFR-08 target of 68). The Georgian translation of the items must be noted as a limitation, since a validated Georgian SUS was not found.

## Gaps to close before P8-T1

- Confirm the items marked **check**.
- Search for a validated Georgian SUS translation; if none, document the translation procedure.
- One more source on Socratic questioning as pedagogy (for example Paul and Elder's work on Socratic questioning) if the supervisor wants a non-computing reference.
- Look for 2025–2026 work evaluating RAG in Georgian or other Kartvelian languages; none found in this scan.
